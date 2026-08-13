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
    /// Imports question bank CSV files into QuestionBank/QuestionData ScriptableObject assets.
    ///
    /// Source: Assets/QuestionBanks/CSV/**/*.csv
    /// Output: Assets/QuestionBanks/Generated/
    ///
    /// FILENAME CONVENTION (see project notes):
    ///   AIHE-AIHEKORI-anything_tN_anything.csv
    ///   e.g. LAA-PER-laakehoito_t1_tietovisa_perustiedot.csv
    ///   -> aihe = LAA, aihekori = PER, tier = T1
    ///
    /// CSV COLUMNS (header row required, must match exactly):
    ///   Tunniste, Kysymys, A, B, C, D, Oikea vastaus,
    ///   Palaute oikein, Palaute väärin, Perustelu, Ammattilaisen merkki
    ///
    /// "Oikea vastaus" FORMAT:
    ///   T1              -> single letter, e.g. "B"
    ///   T2/T3, multiple  -> comma-separated letters, e.g. "A,C"
    ///   T3, zero correct -> literal keyword "EI YHTAAN" (accents optional, case-insensitive)
    ///
    /// When exporting from Excel, use "CSV UTF-8 (Comma delimited)" to keep ä/ö/å intact.
    /// </summary>
    public static class QuestionBankImporter
    {
        private const string SourceFolder = "Assets/QuestionBanks/CSV";
        private const string OutputFolder = "Assets/QuestionBanks/Generated";
        private const string ThemesFolder = OutputFolder + "/Themes";

        private static readonly Regex TierPattern = new Regex(@"_t([1-3])_", RegexOptions.IgnoreCase);

        private static readonly string[] RequiredColumns =
        {
            "Tunniste", "Kysymys", "A", "B", "C", "D", "Oikea vastaus",
            "Palaute oikein", "Palaute väärin", "Perustelu"
        };

        [MenuItem("Nursing Quiz/Import Question Banks")]
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

            int importedBanks = 0;
            int importedQuestions = 0;
            var errors = new List<string>();

            foreach (var path in csvFiles)
            {
                try
                {
                    var bank = ImportFile(path, errors);
                    if (bank != null)
                    {
                        importedBanks++;
                        importedQuestions += bank.questions.Count;
                    }
                }
                catch (Exception e)
                {
                    errors.Add($"{Path.GetFileName(path)}: FAILED - {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Import complete: {importedBanks} bank(s), {importedQuestions} question(s) imported.");

            if (errors.Count > 0)
            {
                Debug.LogWarning(
                    $"{errors.Count} issue(s) found during import (rows below were skipped, not silently dropped):\n"
                    + string.Join("\n", errors));
            }
        }

        private static QuestionBank ImportFile(string path, List<string> errors)
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
            if (rows.Count < 2)
            {
                errors.Add($"{fileName}: file has no data rows. Skipped.");
                return null;
            }

            // The header row isn't always the first line -- sheets commonly have a leading
            // "Oppimistavoite: ..." line above the real column headers. Find whichever row
            // actually contains "Tunniste" as a cell and treat that as the header.
            int headerIndex = rows.FindIndex(r => r.Any(cell => cell.Trim().Equals("Tunniste", StringComparison.OrdinalIgnoreCase)));
            if (headerIndex == -1)
            {
                var delimName = delimiter == '\t' ? "TAB" : delimiter.ToString();
                errors.Add($"{fileName}: no row containing a 'Tunniste' column was found anywhere in the file. " +
                           $"Detected delimiter: '{delimName}'. First row found: [{string.Join(" | ", rows[0])}]. Skipped entire file.");
                return null;
            }

            var col = BuildColumnMap(rows[headerIndex]);
            var missing = RequiredColumns.Where(c => !col.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                var delimName = delimiter == '\t' ? "TAB" : delimiter.ToString();
                errors.Add($"{fileName}: missing required column(s): {string.Join(", ", missing)}. " +
                           $"Detected delimiter: '{delimName}'. Header row found at line {headerIndex + 1}: " +
                           $"[{string.Join(" | ", rows[headerIndex])}]. Skipped entire file.");
                return null;
            }

            var objective = ExtractObjective(rows, headerIndex);
            UpsertThemeInfo(aihe, aihekori);

            var bankAssetPath = $"{OutputFolder}/{aihe}-{aihekori}-{tier}.asset";
            var bank = AssetDatabase.LoadAssetAtPath<QuestionBank>(bankAssetPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<QuestionBank>();
                AssetDatabase.CreateAsset(bank, bankAssetPath);
            }

            bank.aihe = aihe;
            bank.aihekori = aihekori;
            bank.tier = tier;
            bank.sourceFileName = Path.GetFileName(path);
            bank.oppimistavoite = objective ?? "";
            bank.questions.Clear();

            var questionsFolder = $"{OutputFolder}/{aihe}-{aihekori}-{tier}_Questions";
            Directory.CreateDirectory(questionsFolder);

            for (int r = headerIndex + 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

                var tunniste = Get(row, col, "Tunniste");
                if (string.IsNullOrWhiteSpace(tunniste))
                {
                    errors.Add($"{fileName} row {r + 1}: missing Tunniste. Row skipped.");
                    continue;
                }

                var options = new List<string>
                {
                    Get(row, col, "A"), Get(row, col, "B"), Get(row, col, "C"), Get(row, col, "D")
                };
                if (options.Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add($"{fileName} {tunniste}: one or more of columns A-D is empty. Row skipped.");
                    continue;
                }

                if (!TryParseCorrectAnswer(Get(row, col, "Oikea vastaus"), out var correctIndices))
                {
                    errors.Add($"{fileName} {tunniste}: could not parse 'Oikea vastaus' value. Row skipped.");
                    continue;
                }

                if (tier != DifficultyTier.T3 && correctIndices.Count == 0)
                {
                    errors.Add($"{fileName} {tunniste}: zero correct answers is only valid on T3. Row skipped.");
                    continue;
                }
                if (tier == DifficultyTier.T1 && correctIndices.Count != 1)
                {
                    errors.Add($"{fileName} {tunniste}: T1 questions must have exactly one correct answer. Row skipped.");
                    continue;
                }

                var assetPath = $"{questionsFolder}/{SanitizeFileName(tunniste)}.asset";
                var q = AssetDatabase.LoadAssetAtPath<QuestionData>(assetPath);
                if (q == null)
                {
                    q = ScriptableObject.CreateInstance<QuestionData>();
                    AssetDatabase.CreateAsset(q, assetPath);
                }

                q.tunniste = tunniste;
                q.aihe = aihe;
                q.aihekori = aihekori;
                q.tier = tier;
                q.questionText = Get(row, col, "Kysymys");
                q.options = options;
                q.correctIndices = correctIndices;
                q.feedbackCorrect = Get(row, col, "Palaute oikein");
                q.feedbackIncorrect = Get(row, col, "Palaute väärin");
                q.perustelu = Get(row, col, "Perustelu");
                q.ammattilaisenMerkki = col.ContainsKey("Ammattilaisen merkki") ? Get(row, col, "Ammattilaisen merkki") : "";

                EditorUtility.SetDirty(q);
                bank.questions.Add(q);
            }

            EditorUtility.SetDirty(bank);
            return bank;
        }

        /// <summary>
        /// Looks through the preamble rows above the header (e.g. a leading "Oppimistavoite: ..."
        /// line) for a cell that starts with "Oppimistavoite" and returns the text after the colon.
        /// Returns null if no such line is present -- not every sheet needs one.
        /// </summary>
        private static string ExtractObjective(List<List<string>> rows, int headerIndex)
        {
            for (int i = 0; i < headerIndex; i++)
            {
                foreach (var cell in rows[i])
                {
                    var trimmed = cell.Trim();
                    if (trimmed.StartsWith("Oppimistavoite", StringComparison.OrdinalIgnoreCase))
                    {
                        var colonIdx = trimmed.IndexOf(':');
                        var text = colonIdx >= 0 ? trimmed.Substring(colonIdx + 1) : trimmed.Substring("Oppimistavoite".Length);
                        return text.Trim();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Creates the theme-level ThemeInfo asset for this aihe+aihekori if it doesn't exist yet.
        /// Only holds the display name -- the learning objective lives per-tier on QuestionBank
        /// instead (see ImportFile), since it's written separately for T1/T2/T3.
        /// </summary>
        private static void UpsertThemeInfo(string aihe, string aihekori)
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

            EditorUtility.SetDirty(theme);
        }

        private static bool TryParseFileName(string fileName, out string aihe, out string aihekori, out DifficultyTier tier)
        {
            aihe = aihekori = null;
            tier = DifficultyTier.T1;

            var firstSegment = fileName.Split('_')[0]; // e.g. "LAA-PER-laakehoito"
            var parts = firstSegment.Split('-');
            if (parts.Length < 2) return false;
            aihe = parts[0];
            aihekori = parts[1];

            // Pad with underscores so the tier marker is always found regardless of position.
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

        private static bool TryParseCorrectAnswer(string raw, out List<int> indices)
        {
            indices = new List<int>();
            if (string.IsNullOrWhiteSpace(raw)) return false;

            raw = raw.Trim();
            if (raw.Equals("EI YHTAAN", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("EI YHTÄÄN", StringComparison.OrdinalIgnoreCase))
            {
                return true; // valid: empty index list = no correct answer
            }

            foreach (var letter in raw.Split(','))
            {
                var l = letter.Trim().ToUpperInvariant();
                int idx = l switch
                {
                    "A" => 0,
                    "B" => 1,
                    "C" => 2,
                    "D" => 3,
                    _ => -1
                };
                if (idx == -1) return false;
                indices.Add(idx);
            }
            return indices.Count > 0;
        }

        private static Dictionary<string, int> BuildColumnMap(List<string> header)
        {
            var map = new Dictionary<string, int>();
            for (int i = 0; i < header.Count; i++)
                map[header[i].Trim()] = i;
            return map;
        }

        private static string Get(List<string> row, Dictionary<string, int> col, string name)
        {
            if (!col.TryGetValue(name, out var idx)) return "";
            return idx < row.Count ? row[idx].Trim() : "";
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static readonly char[] CandidateDelimiters = { ',', ';', '\t' };

        /// <summary>Reads a file as UTF-8, sharing the handle so an open Excel/Explorer lock doesn't fail the read, and strips a leading BOM if present.</summary>
        private static string ReadFileText(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            return text.TrimStart('\uFEFF');
        }

        /// <summary>
        /// Detects the field delimiter by checking which of comma/semicolon/tab appears most
        /// often in the header line. Char-counting (not just presence) is deliberate: Excel's
        /// CSV export honors the OS regional list separator regardless of what the format name
        /// says, so on some systems (e.g. Finnish Windows) even a "CSV UTF-8 (Comma delimited)"
        /// export can come out semicolon- or tab-separated.
        /// </summary>
        private static char DetectDelimiter(string text)
        {
            var firstLineEnd = text.IndexOf('\n');
            var firstLine = (firstLineEnd >= 0 ? text.Substring(0, firstLineEnd) : text).TrimEnd('\r');

            char best = ',';
            int bestCount = 0;
            foreach (var d in CandidateDelimiters)
            {
                int count = firstLine.Count(c => c == d);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = d;
                }
            }
            return best;
        }

        /// <summary>
        /// Minimal RFC4180-style CSV parser: handles quoted fields, delimiter characters inside
        /// quotes, and escaped double quotes (""). Needed because plain string.Split(delimiter)
        /// breaks on any answer text or feedback text that contains the delimiter character.
        /// </summary>
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
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else inQuotes = false;
                    }
                    else field.Append(c);
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == delimiter)
                    {
                        row.Add(field.ToString());
                        field.Clear();
                    }
                    else if (c == '\r')
                    {
                        // skip
                    }
                    else if (c == '\n')
                    {
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row);
                        row = new List<string>();
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
