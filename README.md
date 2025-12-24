# 16Core Character Mapper

16Core Character Mapper is a free tool designed for storytellers—including authors, screenwriters, and game designers—to develop deep and compelling characters. It helps you visualize your characters' personalities by mapping them across 16 core traits.

By placing character bubbles on a chart, you can easily see how different characters compare and interact, making it a valuable tool for understanding their similarities and differences.

### Features
* **Intuitive Interface:** Map characters onto a chart of 16 core personality traits.
* **Customizable Characters:** Add, edit, and delete characters, and adjust their name, color, and bubble size.
* **Export Functionality:** Save your character map as a high-quality PNG image for use in your creative projects, and optionally select specific characters or categories to include in the exported PNG.
* **Trait Notes:** Add and export per-trait notes for characters (exportable as `.txt`).
* **Localization:** The trait grid is available in 9 languages.
* **Donationware Model:** The app is completely free to use, with optional donations.
* **Privacy-Focused:** The application operates entirely on your local computer and does not collect or transmit any of your personal data or user-generated content.

### Disclaimer
This application is a creative tool, not a scientific or diagnostic instrument. It is not intended for psychological assessment, and the personality-related information is provided for creative inspiration only. The terminology used is based on public-domain concepts, primarily drawn from the International Personality Item Pool (IPIP).

### Support & Contact
* **Website:** For more information, visit the official website at https://georgetsirogiannis.com/16corecharactermapper.
* **Donations:** If you find this tool helpful, please consider making an optional donation via PayPal: https://www.paypal.com/donate/?hosted_button_id=9QWZ6U22CL9KA.
* **Email:** For questions, support, or feedback, you can contact me at 16core@georgetsirogiannis.com.

### System Requirements
* **Operating System:** Windows 10 or later, macOS 10.15 (Catalina) or later, or a modern 64-bit Linux distribution.
* **Runtime:** This application is built on the .NET 8.0 platform. You can either install the .NET 8 runtime for your OS or use a self-contained/platform-specific build that includes the runtime. The Windows installer can install the runtime automatically when required; macOS and Linux packages are provided as self-contained binaries when available.
* **Architecture:** x64 is supported; arm64 builds are available where noted in the releases.
* **Display:** For the best user experience, a screen resolution of 1920x1080 or higher is recommended.

---

### Changelog
**Version 1.0.0** (September 12, 2025)
* Initial release of the 16Core Character Mapper.
* Features include creating, editing, and deleting characters.
* Ability to save and load projects.
* Export character map as a PNG image.
* Toggle between light and dark mode themes.
* Interactive tutorial to guide new users.

**Version 1.0.1** (September 12, 2025)
* Added update functionality.

**Version 1.0.2** (September 12, 2025)
* Minor UI fixes.

**Version 1.0.3** (September 13, 2025)
* This is an important maintenance release that fixes the automatic update functionality and improves application stability:
  * Fixed the **"Check for Updates"** functionality. The application will now correctly find and notify you when new versions are available.
  * The installer has been updated and modernized for a smoother installation experience.
  * Numerous behind-the-scenes improvements to the project structure for better reliability.

**Version 1.1.0** (September 17, 2025)
* A major localization update:
  * Added support for multiple languages in the trait grid. You can now choose between English, French, German, Spanish, Portuguese, Italian, Dutch, Polish and Greek.
* Bug fixes
  * Fixed a bug where the Project Title wasn't saved and reverted back to the savefile name the next time the savefile was loaded.
  * Fixed the Update Check functionality. 

**Version 2.0.0** (Yet unreleased / Committed December 24, 2025)
* This is a major update that transitions 16Core Character Mapper to a natively cross-platform architecture:
	* **Native Cross-Platform Support**: The app has been fully migrated from WPF to the Avalonia UI framework. This major architectural shift means 16Core Character Mapper is now natively cross-platform, featuring a completely restructured code-behind to ensure high performance and stability across different operating systems.
	* **Reimagined User Interface**: The UI has been rebuilt from the ground up. While preserving the intuitive design language of v1.1.0, the interface is now optimized for better scaling and a smoother user experience across various display resolutions.
	* **Enhanced File Management**: Introducing the new .16core file extension for project saves. This custom format allows for better file association and organization. To ensure a seamless transition for early adopters, full support for loading and saving "legacy" .json files has been retained.
* The update also introduces several new features to enhance character development and export capabilities:
	* **Questionnaire**: A built-in questionnaire has been added to help determine character placements on the traits grid. This feature provides a science-backed starting point for character development.
	* **Trait Notes**: A dedicated space has been integrated for each personality trait to document character-specific decisions, trait manifestations, or narrative dynamics. All entries can be exported as a .txt file for external reference.
	* **Improved Export Image Functionality**: The PNG export functionality now offers advanced filtering options. Rather than exporting the entire project by default, the application allows for the selection of specific character categories or a custom list of individual characters to be included in the exported map.