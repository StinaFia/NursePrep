
# Content Management Guide

This guide explains how to add and update the question content of the game. Adding content does not require editing the program code, but it does require the Unity Editor.

> **Note:** The question content and the CSV column names are in Finnish. The column names must be used exactly as listed below.

## Content structure

The game content is organised into four levels:

| Level | Example | In code |
|---|---|---|
| Category | Medication calculations / Lääkelaskut (`LAS`) | `category` |
| Theme | Unit conversions / Yksikkömuunnokset (`YKM`) | `theme` |
| Difficulty tier | Tier 1–3 | `tier` (`T1`, `T2`, `T3`) |
| Question | `LAS-YKM-001` | `QuestionData` |

A theme is identified by the combination of category and theme codes, so the same theme code can be used in several categories (e.g. `LAA-YKM` and `LAS-YKM`) without the questions getting mixed up.

### Difficulty tiers

| Tier | Name shown in the game | Correct answers |
|---|---|---|
| T1 | Perustiedot & muistaminen (Basics and recall) | Exactly one |
| T2 | Ammatillinen päätöksenteko (Professional decision-making) | One or more |
| T3 | Kokemuksellinen oppiminen (Experiential learning) | One or more, or none |

T2 and T3 are always presented as multi-select questions (several options can be selected), so that the number of correct answers is not revealed before the player answers.

## Folder structure (content)

```
Assets/
└─ QuestionBanks/
   ├─ CSV/                        (source files)
   │  ├─ Laakehoito/
   │  └─ Laakelaskut/             
   └─ Generated/        (assets created by the Importer)
      ├─ Themes/        (ThemeInfo assets)
      ├─ <CATEGORY>-<THEME>-<TIER>.asset      (QuestionBank)
      └─ <CATEGORY>-<THEME>-<TIER>_Questions/ (QuestionData)
```

- Tip: CSV files may be organised into subfolders (for example Laakehoito/ and Laakelaskut/). The importer scans all subfolders automatically.

## Steps

Workflow summary:

1. Create or update a CSV file
2. Save it in the CSV folder
3. Run the importer
4. Configure display names
5. Link generated assets to GameFlowController
6. Test the new content

### 1. Write the questions in a spreadsheet

The column names must be exactly as follows. The row above the header row may contain the learning objective in the form `Oppimistavoite: ...`.

| Column | Required | Description |
|---|---|---|
| `Tunniste` | yes | Unique identifier of the question, e.g. `LAS-YKM-001`. Must be unique within the file |
| `Kysymys` | yes | The question text |
| `A`, `B`, `C`, `D` | yes | Answer options, all four must be filled in |
| `Oikea vastaus` | yes | The correct answer, see below |
| `Palaute oikein` | yes | Feedback for a correct answer |
| `Palaute väärin` | yes | Feedback for an incorrect answer |
| `Perustelu` | yes | Explanatory rationale for the correct answer |
| `Ammattilaisen merkki` / `Laskuvinkki` | no | Additional tip

**Format of the `Oikea vastaus` column:**

- T1: a single letter, e.g. `B`
- T2/T3, several correct answers: letters separated by commas, e.g. `A,C`
- T3, no correct answer: `EI YHTÄÄN` (diacritics and letter case are optional)

Example (semicolon as the delimiter):

```csv
Oppimistavoite: Opiskelija osaa muuntaa yksiköitä oikein.
Tunniste;Kysymys;A;B;C;D;Oikea vastaus;Palaute oikein;Palaute väärin;Perustelu
LAS-YKM-001;Kuinka monta milligrammaa on yksi gramma?;10 mg;100 mg;1000 mg;10 000 mg;C;Oikein!;Ei aivan.;1 g = 1000 mg.
```

### 2. Save the CSV with the right name in the right folder

- Format: **UTF-8-encoded CSV**, so that Finnish special characters (ä, ö) are transferred correctly
- Folder: `Assets/QuestionBanks/CSV` (subfolders are allowed)
- The file name determines the category, theme and difficulty tier:

```
<CATEGORY>-<THEME>-<anything>_t<1-3>_<anything>.csv
```

Example: `LAA-PER-laakehoito_t1_tietovisa_perustiedot.csv` is interpreted as category `LAA`, theme `PER` and tier `T1`.

The delimiter (comma, semicolon or tab) is detected from the file automatically.

### 3. Import the questions into Unity

In the Unity Editor menu, select **Nursing Quiz > Import Question Banks**. Check the result of the import in the Console window. Invalid files and rows are skipped and reported in the Console.

Checks performed during the import:

- the file name follows the naming convention
- all required columns are present
- `Tunniste` and all options A–D are filled in
- `Oikea vastaus` can be interpreted
- a T1 question has exactly one correct answer
- zero correct answers is allowed only on tier T3

### 4. Add the display names by hand

The CSV files do not contain the names shown to the player, so they are defined in the Editor:

- **Theme name:** `Assets/QuestionBanks/Generated/Themes/<CATEGORY>-<THEME>.asset` > the *Display Name* field. The import initially sets the name to the theme code and keeps a name written by hand when the import is run again.
- **Category name and description:** the *Category names* list of the `GameFlowController` component in the scene (fields *Category*, *Display Name* and *Description*). The list is optional: if no name is defined, the button shows the category code.

### 5. Link the assets to the game

The importer creates assets but does not automatically register them in the game. Drag the following into the *Content* lists of the `GameFlowController` component:

- the new **ThemeInfo** assets into the list *Available Themes*
- the new **QuestionBank** assets into the list *Available Banks*

Save the scene.

### 6. Test

Play through the new theme at all imported difficulty tiers. A difficulty tier button can be selected only if questions have been imported for that theme and tier.

## Important notes

- **Do not edit the assets in the `Generated` folder by hand** (the only exception is the *Display Name* of ThemeInfo). The import overwrites them. Make corrections in the CSV file and import again.
- **The import does not delete old assets.** If a question is removed from the CSV, its QuestionData asset remains in the folder even though it no longer belongs to any question bank. Delete unnecessary assets by hand.
- **`Tunniste` is the file name of the question asset.** Two identical identifiers in the same file end up in the same asset, so the latter overwrites the former without an error message.
- **The learning objective is read from the first file found.** If the files of different tiers of the same theme contain different text, the Console shows a warning and the first version is kept.
- **Published builds:** Question content is stored in Unity assets and included in the application build. Updating content in a published version of the game therefore requires creating and distributing a new build.

## Troubleshooting

### The theme does not appear in the game
- The CSV file name follows the naming convention
- The import completed without errors
- The generated ThemeInfo asset has been added to GameFlowController > Available Themes
- The generated QuestionBank asset has been added to GameFlowController > Available Banks.

