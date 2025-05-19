APKdevastate
Show Image
Show Image
APKdevastate is a powerful Windows application designed to analyze Android APK files for security risks, malware signatures, and suspicious behaviors. The tool helps identify potentially malicious applications by examining permissions, certificate information, and known Remote Access Trojan (RAT) signatures.
Features

APK Decompilation: Extracts and analyzes APK file contents
Permission Analysis: Lists and evaluates dangerous Android permissions
Certificate Verification: Validates APK signing certificates against trusted organizations
RAT Detection: Scans for known Remote Access Trojan signatures
Hash Generation: Calculates MD5, SHA1, and SHA256 hashes for file verification
Encryption Detection: Identifies potentially obfuscated or encrypted code
Risk Assessment: Provides an overall security evaluation of the analyzed APK

Screenshots
[Screenshots will be added soon]
Requirements

Windows 7/8/10/11
.NET Framework 4.5+
Java Runtime Environment (JRE)

Installation

Download the latest release from the Releases page
Extract the ZIP file to your preferred location
Run the APKdevastate.exe file

Usage

Open the application
Click "Select APK" to choose an Android application file
Click "Analyze" to begin the security scan
Review the detailed analysis results

Technical Details
APKdevastate leverages several tools and techniques to analyze APK files:

APKTool: For decompiling APK resources
AAPT: For extracting package information
APKSigner: For certificate verification
Regex Pattern Matching: For identifying suspicious code patterns
Certificate Trust Verification: Against a database of known legitimate organizations

Known RATs Detected
The tool can identify multiple known Android RAT families, including:

SpyNote
SpyMax
CraxsRAT
CellikRAT
InsomniaSpy
CypherRAT
EagleSpy
G-700RAT
ENCCN
BrataRAT
EverSpy
BlackSpy
BigSharkRAT

Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

Fork the repository
Create your feature branch (git checkout -b feature/amazing-feature)
Commit your changes (git commit -m 'Add some amazing feature')
Push to the branch (git push origin feature/amazing-feature)
Open a Pull Request

License
This project is licensed under the MIT License - see the LICENSE file for details.
Acknowledgments

Created by Rafig Zarbaliyev
Instagram: @rafok2v9c

Disclaimer
This tool is intended for security research and legitimate application verification only. Always respect privacy and applicable laws when analyzing applications. The authors are not responsible for any misuse of this software.
Contact
For questions, feature requests, or bug reports, please open an issue on this repository.

⭐ Star this repository if you find it useful!
