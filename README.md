# Pero 🪶

A smart assistant for Ukrainian writing. Pero helps you find and fix grammar, punctuation, and style errors in real time.

## What is Pero?

Pero is a browser extension that makes your Ukrainian writing clear and professional. It works on most websites, checking your text in emails, social media posts, and documents as you type.

### Features

*   **Real-Time Correction:** See mistakes immediately, with no delays.
*   **Contextual Suggestions:** Pero underlines the full context of an error, not just a single character, making suggestions clear and intuitive.
*   **Punctuation Rules:** Correct common errors like missing spaces after commas or extra spaces before punctuation.
*   **Minimalist Interface:** The tooltip is compact and clean, showing you suggestions without getting in the way.
*   **Rule Explanations:** Click the info icon on any suggestion to understand *why* it was flagged.

## Installation (for Google Chrome Users)

1.  **Download:** Go to the [Releases page](https://github.com/your-username/pero/releases) and download the latest `Pero_vX.X.X.zip` file.
2.  **Unzip:** Unzip the downloaded file. You will get a folder named `Pero_vX.X.X`.
3.  **Open Chrome Extensions:** Open a new tab in Chrome and go to `chrome://extensions`.
4.  **Enable Developer Mode:** In the top-right corner of the Extensions page, turn on the "Developer mode" switch.
5.  **Load the Extension:** Click the "Load unpacked" button that appears.
6.  **Select the Folder:** In the file selection window, choose the `Pero_vX.X.X` folder that you unzipped in step 2.

Pero is now installed and ready to use. You will see its icon in your browser's toolbar.

## Contributing to Development

We welcome contributions to make Pero even better. To get started, follow these steps.

### Setup

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/pero.git
    ```

2.  **Navigate to the project directory:**
    ```bash
    cd pero
    ```

3.  **Install dependencies:**
    ```bash
    npm install
    ```

### Running in Development Mode

To build the extension and have it automatically re-build when you make changes, run:

```bash
npm run dev
```

This command creates a `dist` folder in the project root. This is the folder you will load into Chrome for testing.

### Loading for Development

Follow the same steps as the user installation, but in step 6, select the `dist` folder from your local project directory instead of the release folder.

Now you can make changes to the source code, and they will be reflected after the `dev` script rebuilds the project.