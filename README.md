# CofA Importer

CofA Importer is a Windows Presentation Foundation (WPF) application designed to automate the import and processing of 
Certificates of Analysis (CofA) for internal IND/ICC workflows. It provides a guided interface for selecting the input file, 
running the import, tracking real-time progress, and generating a detailed log at the end of the process.

---

## 🚀 Features

- **Automated CofA Import Process**  
  Reads Excel files, processes product attachment data, and updates the target SQL Server database.

- **Streaming Real-Time Output**  
  Uses a buffered logging mechanism to avoid UI freezing while displaying continuous progress in the output textbox.

- **User-Writable Settings**  
  Stores configuration such as database connection info and file paths in a writable `appsettings.json` located under the user's AppData folder.

- **Log Prompt Dialog**  
  After an import runs, the application offers to open the generated log file.  
  Includes a **"Don't ask me again"** option with persistent storage.

- **Installer Support**  
  Designed to be packaged using **Visual Studio Installer Projects** for deployment to Windows 10/11 and Server 2022 systems.

---

## 🛠 Technologies Used

- **.NET 8 (WPF)**
- **C#**
- **EPPlus** for Excel processing
- **SqlClient** for database operations
- **XAML** for UI layout
- **Custom AppSettingsService** for persistent configuration

---

## 📂 Project Structure (General Overview)

CofA_Importer/
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / MainWindow.xaml.cs
├── Import/
│ └── CofaData.cs
├── Services/
│ └── AppSettingsService.cs
├── Logging/
│ └── BufferedLogger.cs
├── Dialogs/
│ └── LogPrompt.xaml
├── appsettings.json (user-writable)
└── README.md


---

## 🧰 How to Build and Run

1. Clone the repository:
   ```bash
   git clone https://github.com/Caniac217/CofA_Importer.git


Open the solution in Visual Studio 2022.

Restore NuGet packages automatically when prompted.

Build and run the application.

⚠️ Notes

The import process clears the ProductAttachments table before inserting new CofA data.

The application handles long-running imports using buffered logging to prevent UI freezes.

Behavior of the Settings window is designed for first-run initialization scenarios.

📄 License

Internal ICC tool — no public license specified.