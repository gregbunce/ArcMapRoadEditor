This project creates the ArcMap toolbar that's used during the SGID road update process. The tools on this toolbar are used when a UGRC data editor is iterating through a county's data submission. These tools help the data editor determine what new county segments and edits to accept into the UTRANS database. The tools allow the UGRC editor to visually view and accept a county's segment-based edits/changes and then import them, while also including the required SGID schema information such as muni, zipcode, address system, cartocode, county, etc.      

This C#.net project uses ESRI's ArcObjects (ArcMap 10.7.x). This project contains an ArcMap toolbar with 4 tools. 

This project also requires the .NET setup project (VS Installer package) to create the installation files (.msi). You can access the setup solution on the google drive.
https://learn.microsoft.com/en-us/visualstudio/deployment/installer-projects-net-core?view=vs-2022
https://drive.google.com/drive/u/1/folders/1iphCVmjy2-aTXAmGSGRHHYRsmLw_DUz0

Existing installation files can be found on the google drive
https://drive.google.com/drive/u/1/folders/1roVWD108iz_okvm08f65_rLQWyZDcmla

My notes on ArcObjects and custom tools can be found on google drive
https://drive.google.com/drive/folders/1qWyvIf2d2rrg4rbDwEZFEA4M2VaoZ-WO?usp=sharing
