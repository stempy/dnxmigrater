# dnxmigrater
DotNet .csproj based Project Migrater to project.json migrater

This console app is designed to migrate C# .Net 4.6 and below Projects to the new vNext VS2015 project.json format for dnx.

ALPHA
Very early stages, currently creates projects off a .csproj or .sln file.
This does not modify any existing files, it creates a new directory structure with the new project files, and updated .sln file

project.json / appsettings.json / projectname.xproj

Usage:
    dnxmigrater solutionfile.sln [destdir] [/includefiles]
    
   [destdir] defaults to your %TEMP% directory if not specified
   [/includefiles] copy all files included in .csproj file to new project dir, currently a switch to test out process

Current Issues:
    not all dependencies are able to be resolved for all projects
    