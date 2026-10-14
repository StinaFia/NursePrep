# 🩺 NursePrep

NursePrep is a Unity-based learning game designed for nursing students. It helps students practise and review professional knowledge independently. 

The application was developed for the Testbed project at Lapland University of Applied Sciences.

<!-- [TODO: add a screenshot of the game, e.g. ![Theme selection](docs/images/screenshot.png)] -->

> **Note:** The game interface and question content are in Finnish.

## ✨ Features

- Content organised by category, theme and difficulty tier (T1–T3)
- Multiple-choice questions with answer options A–D
- Immediate explanatory feedback after each answer
- Result screen with score, star rating and performance feedback
- CSV-based content workflow for adding and updating questions without code changes

## 🛠️ Technologies

- Unity 6.3 LTS (6000.3.17f1)
- C#
- Visual Studio
- Git and GitHub

## 🚀 Getting Started

1. Install Unity **6.3 LTS (6000.3.17f1)** through Unity Hub.
2. Clone this repository.
3. Open the project in Unity Hub (**Add > Add project from disk**).
4. Open the main scene **[TODO: scene name]** and press Play.

## 📊 Content Workflow

Question content is maintained in CSV files and imported into Unity using: **Unity Editor Menu > Nursing Quiz > Import Question Banks** 

The importer generates the required assets. Generated assets are then linked manually to the lists of the `GameFlowController` component. The exact steps are described in the content guide.

## 📋 Documentation

- [Adding content](docs/content-guide.en.md): CSV structure, naming conventions and import workflow
- [Architecture and further development](docs/architecture.en.md): project structure, design decisions and future development notes

## 💼 Project Status

The first playable version of NursePrep is complete and includes the multiple-choice game mode. The code structure allows further game modes to be added.

Ideas for future game modes:
- Matching Pairs
- Memory Card
- Correct Order
- Experience-based Learning

## 🧮 Disclaimer

The question content is intended as study support and is not clinical guidance.

## 👥 Author and licence

- Author: **[TODO]**
- Commissioned by: Lapland University of Applied Sciences, Testbed project
- Licence: **[TODO]**
