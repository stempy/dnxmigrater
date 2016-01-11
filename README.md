# dnxmigrater
DotNet .csproj based Project Migrater to project.json migrater

This console app is designed to migrate C# .Net 4.6 Projects to the new vNext VS2015 project.json format for dnx. 

The goal of this migrater is to migrate project files up to ASP.NET 5's DNX approach, and MVC6 structure, 
this means renaming namespaces, usings, class names, removing dependencies on System.Web. There will still be manual work involved,
however this is designed automate as much as possible. No existing files are modified, everything is created/copied into a new directory.

There is still much work to be done on MVC based projects that rely on System.Web, as MVC6 no longer uses it.

project.json / appsettings.json / projectname.xproj

Usage:
    dnxmigrater solutionfile.sln [destdir] [/includefiles]
    
   [destdir] defaults to your %TEMP% directory if not specified
   [/includefiles] copy all files included in .csproj file to new project dir, currently a switch to test out process

Current Issues:
    Projects relying on System.Web or any MVC5 (and below) specific dependencies
    