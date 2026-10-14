# Architecture and Future Development

This document describes the code structure of the game, its limitations and ideas for further development.

<!-- [TODO: add the diagrams to the docs/images folder, e.g. ![Game structure](images/pelin_rakenne.png)] -->

## ⌨ Code structure

Namespaces:

| Namespace | Contents |
|---|---|
| `NursingQuiz.Core` | Shared enums, interfaces and base classes, e.g. `DifficultyTier`, `GameModeType` |
| `NursingQuiz.Data` | ScriptableObject content assets: `QuestionData`, `QuestionBank`, `ThemeInfo` |
| `NursingQuiz.UI` | Application flow and menu UI, e.g. `GameFlowController`, `CategoryInfo`, `ThemeButtonUI` |
| `NursingQuiz.GameModes.MultipleChoice` | Multiple-choice implementation: `MultipleChoiceView`, `MultipleChoiceController`, `AnswerOptionUI`, `ResultView` |
| `NursingQuiz.GameModes.MatchPairs` | Match pairs gameplay implementation (not in use) |
| `NursingQuiz.EditorTools` | CSV import tools used in Unity Editor: `QuestionBankImporter`, `MatchPairImporter`|

🗝️ Key classes:

- **`QuestionBankImporter`**: reads the CSV files and creates ScriptableObject assets from them. Updates existing assets instead of creating copies.
- **`GameFlowController`**: the game's control component. Manages the transitions from the main menu to category selection, theme and difficulty selection, and the game view. Builds the category and theme buttons from the ThemeInfo assets.
- **`ThemeInfo`**: stores category and theme metadata, including the display name and learning objective (`category`, `theme`, `displayName`, `oppimistavoite`, i.e. the learning objective).
- **`QuestionBank`**: the questions of one category, theme and difficulty tier (`category`, `theme`, `tier`, `questions`).
- **`QuestionData`**: a single question with its answer options, correct answers and feedback.
- **`MultipleChoiceView`**: connects the multiple-choice game logic (`MultipleChoiceController`) to the Unity UI. Shows the question and the answer options, handles the player's choices, shows feedback and moves to the result screen at the end of the round.
- **`ResultView`**: the result screen of a round. Shows the theme, the score, star rating and a feedback message. The class only handles presentation and contains no gameplay or scoring logic.

### 🎮 Game flow

```
Main menu → Category selection → Theme and Difficulty selection → Game view → Results
```

The number of questions in a round is set in the *Questions Per Round* field of the `GameFlowController` component (default 5).

### Multiple-choice game mode

`GameFlowController` starts a round by calling `MultipleChoiceView.BeginRound` with the selected category and difficulty tier and the question banks of the selected theme. `MultipleChoiceView` requires a `MultipleChoiceController` component on the same object, which manages the logic of the round (selecting questions, evaluating answers and counting points) -> the view and the logic are separate from UI presentation.

**Answering by difficulty tier**

| Tier | Answering |
|---|---|
| T1 | Clicking an option evaluates the answer immediately |
| T2 | Several options can be selected and the player confirms the selection with the **Vastaa** (Answer) button. At least one option must be selected |
| T3 | Like T2, but the Vastaa button can also be pressed without a selection (no correct answer) |

**After answering**

- The options are locked and coloured: a correctly selected option darker green, a correct option that was not selected lighter green and an incorrectly selected option red.
- The feedback panel shows an icon (correct or incorrect ✅ / ❌), the feedback text for a correct or incorrect answer, and the rationale (`rationale`).  An optional professional tip (`professionalPatch`) is also shown when available.
- The main button changes to **Seuraava** (Next) or, after the last question, **Katso tulos** (See result).
- Progress is shown in the form *Kysymys 2/10* (Question 2/10).

**Result screen (`ResultView`)**

At the end of a round the screen shows the theme, the score in the form *correct / total* and stars. The number of stars is calculated from the share of correct answers (rounded to the total number of stars), and the unearned stars turn grey and shrink. The feedback message depends on the share of correct answers:

| Correct answers | Message |
|---|---|
| at least 90 % | Excellent performance |
| at least 60 % | Good performance |
| below 60 % | A good start, review the theme and try again |

The **Yritä uudelleen** (try again) button starts a new round with the same category, theme and difficulty tier.

**Notes**

- `MultipleChoiceView` checks at startup that all Inspector references have been assigned. If something is missing, the Console lists the missing fields and the component disables itself.

## ⛔ Known limitations

- Linking the assets to the `GameFlowController` lists is a manual step.
- Category names are defined in the Editor, not in the CSV.
- Unity Editor is required to import and manage new question content.


## 💡 Ideas for further development

- Linking the assets to the game automatically (e.g. the importer adds them to the lists or the game finds them in the folder)
- Reading category names and descriptions from the CSV files
- Cleaning up outdated assets during the import
- Sign up, saving results and tracking progress 
- Additional game modes
