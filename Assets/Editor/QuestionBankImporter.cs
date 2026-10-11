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
    /// -------------------------------------------------------------------------------------------------
    /// FILENAME FORMAT:
    /// -------------------------------------------------------------------------------------------------
    ///   AIHE-AIHEKORI-anything_tN_anything.csv
    ///   e.g. LAA-PER-laakehoito_t1_tietovisa_perustiedot.csv
    ///   -> aihe = LAA, aihekori = PER, tier = T1
    ///
    /// -------------------------------------------------------------------------------------------------
    /// CSV COLUMNS:
    /// -------------------------------------------------------------------------------------------------
    /// Header row required, must match exactly -- may have a preamble row like
    /// "Oppimistavoite: ..." above it, the importer finds the header row wherever it is:
    ///   Tunniste, Kysymys, A, B, C, D, Oikea vastaus,
    ///   Palaute oikein, Palaute väärin, Perustelu, Ammattilaisen merkki/Laskuvinkki
    ///
    /// -------------------------------------------------------------------------------------------------
    /// "Oikea vastaus" (correct answer) FORMAT:
    /// -------------------------------------------------------------------------------------------------
    ///   T1              -> single letter, e.g. "B"
    ///   T2/T3, multiple  -> comma-separated letters, e.g. "A,C"
    ///   T3, zero correct -> literal keyword "EI YHTAAN" (accents optional, case-insensitive)
    ///
    /// Delimiter (comma/semicolon/tab) is auto-detected per file.
    /// </summary>
  

    // Editor-only import tool used to generate question assets from CSV files.
    public static class QuestionBankImporter
    {
        private const string SourceFolder = "Assets/QuestionBanks/CSV";
        private const string OutputFolder = "Assets/QuestionBanks/Generated";
        private const string ThemesFolder = OutputFolder + "/Themes";

        private static readonly Regex TierPattern = new Regex(@"_t([1-3])_", RegexOptions.IgnoreCase);
        private static readonly char[] CandidateDelimiters = { ',', ';', '\t' };

        private static readonly string[] RequiredColumns =
        {
            "Tunniste", "Kysymys", "A", "B", "C", "D", "Oikea vastaus",
            "Palaute oikein", "Palaute väärin", "Perustelu"
        };

        [MenuItem("Nursing Quiz/Import Question Banks")]

        /// <summary>
        /// Imports all CSV files found under the QuestionBanks/CSV folder,
        /// creates or updates QuestionBank assets and refreshes the AssetDatabase.
        /// </summary>

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

        /// <summary>
        /// Imports a single CSV file into a QuestionBank asset.
        /// 
        /// Responsibilities:
        /// - Parse filename metadata (aihe, aihekori, tier)
        /// - Read and validate CSV structure (header row, required columns)
        /// - Create/update ThemeInfo
        /// - Create/update QuestionBank
        /// - Create/update QuestionData assets
        /// </summary>

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

            int headerIndex = rows.FindIndex(r => r.Any(cell => cell.Trim().Equals("Tunniste", StringComparison.OrdinalIgnoreCase)));
            if (headerIndex == -1)
            {
                var delimName = delimiter == '\t' ? "TAB" : delimiter.ToString();
                errors.Add($"{fileName}: no row containing a 'Tunniste' column was found anywhere in the file. " +
                           $"Detected delimiter: '{delimName}'. First row found: [{string.Join(" | ", rows[0])}]. Skipped entire file.");
                return null;
            }

            var columnMap = BuildColumnMap(rows[headerIndex]);
            var missing = RequiredColumns.Where(c => !columnMap.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                var delimName = delimiter == '\t' ? "TAB" : delimiter.ToString();
                errors.Add($"{fileName}: missing required column(s): {string.Join(", ", missing)}. " +
                           $"Detected delimiter: '{delimName}'. Header row found at line {headerIndex + 1}: " +
                           $"[{string.Join(" | ", rows[headerIndex])}]. Skipped entire file.");
                return null;
            }

            var objective = ExtractObjective(rows, headerIndex);
            UpsertThemeInfo(aihe, aihekori, objective, fileName, errors);

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
            bank.questions.Clear();

            var questionsFolder = $"{OutputFolder}/{aihe}-{aihekori}-{tier}_Questions";
            Directory.CreateDirectory(questionsFolder);

            for (int r = headerIndex + 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

                var tunniste = Get(row, columnMap, "Tunniste");
                if (string.IsNullOrWhiteSpace(tunniste))
                {
                    errors.Add($"{fileName} row {r + 1}: missing Tunniste. Row skipped.");
                    continue;
                }

                var options = new List<string>
                {
                    Get(row, columnMap, "A"), Get(row, columnMap, "B"), Get(row, columnMap, "C"), Get(row, columnMap, "D")
                };
                if (options.Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add($"{fileName} {tunniste}: one or more of columns A-D is empty. Row skipped.");
                    continue;
                }

                if (!TryParseCorrectAnswer(Get(row, columnMap, "Oikea vastaus"), out var correctIndices))
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
                var question = AssetDatabase.LoadAssetAtPath<QuestionData>(assetPath);
                if (question == null)
                {
                    question = ScriptableObject.CreateInstance<QuestionData>();
                    AssetDatabase.CreateAsset(question, assetPath);
                }

                question.tunniste = tunniste;
                question.aihe = aihe;
                question.aihekori = aihekori;
                question.tier = tier;
                question.questionText = Get(row, columnMap, "Kysymys");
                question.options = options;
                question.correctIndices = correctIndices;
                question.correctFeedback = Get(row, columnMap, "Palaute oikein");
                question.incorrectFeedback = Get(row, columnMap, "Palaute väärin");
                question.rationale = Get(row, columnMap, "Perustelu");

                // Support both legacy quiz files with "Ammattilaisen merkki" and newer ones with "Laskuvinkki" column.
                string patch = "";
                if (columnMap.ContainsKey("Ammattilaisen merkki")) patch = Get(row, columnMap, "Ammattilaisen merkki");
                if (string.IsNullOrWhiteSpace(patch) && columnMap.ContainsKey("Laskuvinkki")) patch = Get(row, columnMap, "Laskuvinkki");
                question.professionalPatch = patch;
                EditorUtility.SetDirty(question);
                bank.questions.Add(question);
            }

            EditorUtility.SetDirty(bank);
            return bank;
        }

        /// <summary>
        /// Searches rows above the CSV header for an "Oppimistavoite:" declaration and returns the associated objective text if found.
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
        /// Creates or updates a ThemeInfo asset for the given aihe-aihekori combination.
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
                               $"for theme {aihe}-{aihekori}. Keeping the first version found -- align the wording " +
                               "across the T1/T2/T3 files for this theme if that wasn't intentional.");
                }
                else
                {
                    theme.oppimistavoite = objective;
                }
            }

            EditorUtility.SetDirty(theme);
        }

        /// <summary>
        /// Extracts aihe, aihekori and difficulty tier from a CSV filename.
        ///
        /// Expected format:
        /// AIHE-AIHEKORI-xxxx_tN_xxxx.csv
        ///
        /// Example:
        /// LAA-PER-laakehoito_t1_perustiedot.csv
        /// </summary>
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

        /// <summary>
        /// Parses the "Oikea vastaus" column.
        /// 
        /// Supported formats:
        /// A
        /// B
        /// A,C
        /// EI YHTÄÄN
        ///
        /// Returns answer option indices:
        /// A = 0, B = 1, C = 2, D = 3.
        /// </summary>
        private static bool TryParseCorrectAnswer(string raw, out List<int> indices)
        {
            indices = new List<int>();
            if (string.IsNullOrWhiteSpace(raw)) return false;


            // T3 questions may have zero correct answers, indicated by the literal string "EI YHTAAN" (
            raw = raw.Trim();
            if (raw.Equals("EI YHTAAN", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("EI YHTÄÄN", StringComparison.OrdinalIgnoreCase))
            {
                return true;
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

        /// <summary>
        /// Builds a case-insensitive mapping between CSV column names and their column indices.
        /// </summary>
        private static Dictionary<string, int> BuildColumnMap(List<string> header)
        {
            var map = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Count; i++)
                map[header[i].Trim()] = i;
            return map;
        }

        /// <summary>
        /// Safely retrieves a column value from a CSV row.
        /// Returns an empty string if the column is missing.
        /// </summary>
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

        /// <summary>
        /// Reads a UTF-8 encoded CSV file
        /// </summary>
        private static string ReadFileText(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();
            return text.TrimStart('\uFEFF');
        }

        /// <summary>
        /// Attempts to detect the delimiter used by the CSV file (comma, semicolon or tab).
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
        /// Lightweight CSV parser used by the importer.
        ///
        /// Supports:
        /// - quoted fields
        /// - escaped quotes ("")
        /// - commas/semicolons/tabs inside quoted content
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
