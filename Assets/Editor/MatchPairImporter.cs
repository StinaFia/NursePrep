using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using NursingQuiz.Core;
using NursingQuiz.Data;

namespace NursingQuiz.EditorTools
{
    /// <summary>
    /// Imports "Yhdistä oikein" (match the pairs) CSV files into MatchPairSet assets.
    ///
    /// Source: Assets/MatchPairs/CSV/**/*.csv
    /// Output: Assets/MatchPairs/Generated/
    ///
    /// FILENAME CONVENTION (same as question banks):
    ///   AIHE-AIHEKORI-anything_tN_anything.csv
    ///
    /// FILE CONTENT: labeled lines can appear anywhere in the file (before or after the
    /// pairs table), one per line/row -- matching the source Word document's structure:
    ///   Pelin nimi: ...
    ///   Oppimistavoite: ...
    ///   Palaute: ...
    ///   Ammattilaisen merkki: ...
    ///
    /// The pairs table is found by locating the header row containing both "Vasen sarake"
    /// and "Oikea sarake", then reading every following row with two non-empty cells as one
    /// pair, stopping at a blank row or a row starting with one of the labels above.
    ///
    /// IMPORTANT: this importer also creates/updates a ThemeInfo asset for the aihe+aihekori,
    /// in the SAME Themes folder that QuestionBankImporter uses -- the theme+difficulty select
    /// screen is shared across both game modes and only shows themes that have a ThemeInfo
    /// asset, so match-pair content needs one just as much as question banks do.
    /// </summary>
    public static class MatchPairImporter
    {
        private const string SourceFolder = "Assets/MatchPairs/CSV";
        private const string OutputFolder = "Assets/MatchPairs/Generated";
        private const string ThemesFolder = "Assets/QuestionBanks/Generated/Themes"; // shared with QuestionBankImporter

        private static readonly Regex TierPattern = new Regex(@"_t([1-3])_", RegexOptions.IgnoreCase);
        private static readonly char[] CandidateDelimiters = { ',', ';', '\t' };

        private static readonly string[] KnownLabels =
        {
            "Pelin nimi", "Oppimistavoite", "Palaute", "Ammattilaisen merkki"
        };

        [MenuItem("Nursing Quiz/Import Match Pair Sets")]
        public static void ImportAll()
        {
            if (!Directory.Exists(SourceFolder))
            {
                Debug.LogError($"Source folder not found: {SourceFolder}. Create it and drop CSV files in.");
                return;
            }

            Directory.CreateDirectory(OutputFolder);

            var csvFiles = Directory.GetFiles(SourceFolder, "*.csv", SearchOption.AllDirectories);
            if (csvFiles.Length == 0)
            {
                Debug.LogWarning($"No CSV files found under {SourceFolder}.");
                return;
            }

            int importedSets = 0;
            int importedPairs = 0;
            var errors = new List<string>();

            foreach (var path in csvFiles)
            {
                try
                {
                    var set = ImportFile(path, errors);
                    if (set != null)
                    {
                        importedSets++;
                        importedPairs += set.pairs.Count;
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"{Path.GetFileName(path)}: FAILED - {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Import complete: {importedSets} match-pair set(s), {importedPairs} pair(s) imported.");

            if (errors.Count > 0)
                Debug.LogWarning($"{errors.Count} issue(s) found during import:\n" + string.Join("\n", errors));
        }

        private static MatchPairSet ImportFile(string path, List<string> errors)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);

            if (!TryParseFileName(fileName, out var aihe, out var aihekori, out var tier))
            {
                errors.Add($"{fileName}: could not parse AIHE-AIHEKORI prefix or _tN_ tier marker from filename. Skipped entire file.");
                return null;
            }

            var text = ReadFileText(path);
            var delimiter = DetectDelimiter(text);
            var rows = ParseCsv(text, delimiter);

            int headerIndex = rows.FindIndex(r =>
                r.Any(c => c.Trim().Equals("Vasen sarake", StringComparison.OrdinalIgnoreCase)) &&
                r.Any(c => c.Trim().Equals("Oikea sarake", StringComparison.OrdinalIgnoreCase)));

            if (headerIndex == -1)
            {
                errors.Add($"{fileName}: no row containing both 'Vasen sarake' and 'Oikea sarake' was found. Skipped entire file.");
                return null;
            }

            var labels = ExtractLabels(rows);

            // Create/update the shared ThemeInfo for this aihe+aihekori, same as QuestionBankImporter --
            // without this, the theme never appears as selectable on the theme+difficulty screen.
            var objective = labels.TryGetValue("Oppimistavoite", out var obj) ? obj : null;
            UpsertThemeInfo(aihe, aihekori, objective, fileName, errors);

            var pairs = new List<MatchPairEntry>();
            for (int r = headerIndex + 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) break;

                var left = row.Count > 0 ? row[0].Trim() : "";
                var right = row.Count > 1 ? row[1].Trim() : "";

                if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right)) break;
                if (IsLabeledLine(left)) break;

                pairs.Add(new MatchPairEntry { left = left, right = right });
            }

            if (pairs.Count == 0)
            {
                errors.Add($"{fileName}: no pairs found under the header row. Skipped entire file.");
                return null;
            }

            var assetPath = $"{OutputFolder}/{aihe}-{aihekori}-{tier}.asset";
            var set = AssetDatabase.LoadAssetAtPath<MatchPairSet>(assetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<MatchPairSet>();
                AssetDatabase.CreateAsset(set, assetPath);
            }

            set.aihe = aihe;
            set.aihekori = aihekori;
            set.tier = tier;
            set.sourceFileName = Path.GetFileName(path);
            set.gameName = labels.TryGetValue("Pelin nimi", out var gn) ? gn : "";
            set.oppimistavoite = objective ?? "";
            set.completionFeedback = labels.TryGetValue("Palaute", out var pf) ? pf : "";
            set.professionalPatch = labels.TryGetValue("Ammattilaisen merkki", out var am) ? am : "";
            set.pairs = pairs;

            EditorUtility.SetDirty(set);
            return set;
        }

        /// <summary>
        /// Creates or updates the theme-level ThemeInfo asset for this aihe+aihekori, in the
        /// SAME folder QuestionBankImporter uses -- so a theme that already exists from a
        /// question bank (or vice versa) isn't duplicated, and match-pair-only themes get a
        /// ThemeInfo of their own. If the objective conflicts with one already set, the
        /// existing text is kept and a warning is logged, same policy as QuestionBankImporter.
        /// </summary>
        private static void UpsertThemeInfo(string aihe, string aihekori, string objective, string fileName, List<string> errors)
        {
            Directory.CreateDirectory(ThemesFolder);
            var path = $"{ThemesFolder}/{aihe}-{aihekori}.asset";
            var theme = AssetDatabase.LoadAssetAtPath<ThemeInfo>(path);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ThemeInfo>();
                AssetDatabase.CreateAsset(theme, path);
            }

            theme.aihe = aihe;
            theme.aihekori = aihekori;
            if (string.IsNullOrEmpty(theme.displayName))
                theme.displayName = aihekori;

            if (!string.IsNullOrWhiteSpace(objective))
            {
                if (!string.IsNullOrEmpty(theme.oppimistavoite) && theme.oppimistavoite != objective)
                {
                    errors.Add($"{fileName}: this file's Oppimistavoite text differs from one already imported " +
                               $"for theme {aihe}-{aihekori}. Keeping the first version found.");
                }
                else
                {
                    theme.oppimistavoite = objective;
                }
            }

            EditorUtility.SetDirty(theme);
        }

        private static bool IsLabeledLine(string cell)
        {
            return KnownLabels.Any(l => cell.StartsWith(l, StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, string> ExtractLabels(List<List<string>> rows)
        {
            var result = new Dictionary<string, string>();
            foreach (var row in rows)
            {
                foreach (var cell in row)
                {
                    var trimmed = cell.Trim();
                    foreach (var label in KnownLabels)
                    {
                        if (trimmed.StartsWith(label, StringComparison.OrdinalIgnoreCase) && !result.ContainsKey(label))
                        {
                            var colonIdx = trimmed.IndexOf(':');
                            var value = colonIdx >= 0 ? trimmed.Substring(colonIdx + 1) : trimmed.Substring(label.Length);
                            result[label] = value.Trim();
                        }
                    }
                }
            }
            return result;
        }

        private static bool TryParseFileName(string fileName, out string aihe, out string aihekori, out DifficultyTier tier)
        {
            aihe = aihekori = null;
            tier = DifficultyTier.T1;

            var firstSegment = fileName.Split('_')[0];
            var parts = firstSegment.Split('-');
            if (parts.Length < 2) return false;
            aihe = parts[0];
            aihekori = parts[1];

            var match = TierPattern.Match("_" + fileName + "_");
            if (!match.Success) return false;

            tier = match.Groups[1].Value switch
            {
                "1" => DifficultyTier.T1,
                "2" => DifficultyTier.T2,
                "3" => DifficultyTier.T3,
                _ => DifficultyTier.T1
            };
            return true;
        }

        private static string ReadFileText(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            return text.TrimStart('\uFEFF');
        }

        private static char DetectDelimiter(string text)
        {
            var firstLineEnd = text.IndexOf('\n');
            var firstLine = (firstLineEnd >= 0 ? text.Substring(0, firstLineEnd) : text).TrimEnd('\r');

            char best = ',';
            int bestCount = 0;
            foreach (var d in CandidateDelimiters)
            {
                int count = firstLine.Count(c => c == d);
                if (count > bestCount) { bestCount = count; best = d; }
            }
            return best;
        }

        private static List<List<string>> ParseCsv(string text, char delimiter)
        {
            var rows = new List<List<string>>();
            var field = new StringBuilder();
            var row = new List<string>();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == delimiter) { row.Add(field.ToString()); field.Clear(); }
                    else if (c == '\r') { }
                    else if (c == '\n') { row.Add(field.ToString()); field.Clear(); rows.Add(row); row = new List<string>(); }
                    else field.Append(c);
                }
            }
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); rows.Add(row); }
            return rows;
        }
    }
}