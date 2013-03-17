using System;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Runtime.InteropServices;

namespace ETH.Zonnon.Compiler
{
	[ComVisible(true), Guid("0A9ACB40-50F6-4bf5-BE54-1B44F13AD357")]
	public sealed class Registrar : Microsoft.VisualStudio.Package.RegistrationInfo
	{
		public Registrar()
		{
          //this.DebuggerEEGuid = "{"+typeof(DebuggerEE).GUID+"}";
          //this.DebuggerLanguageGuid = "{"+typeof(DebuggerLanguage).GUID+"}";
            this.LanguageServiceType = typeof(LanguageService);
			this.LanguageName = "ETH.Zonnon";
			this.LanguageShortName = "Zonnon";
			this.SourceFileExtension = "znn";
            this.ProjectFileExtension = "znnproj";
			this.Win32ResourcesDllPath = Path.GetDirectoryName(this.GetType().Assembly.Location)+Path.DirectorySeparatorChar+
                                         "1033"+Path.DirectorySeparatorChar+"ETH.Zonnon.Resources.dll";
      
           this.PackageType = typeof(Package);
           this.ProductName = "ETH Zonnon";
           this.PackageName = "Zonnon";
           this.ProductShortName = "ETH Zonnon";
         //this.CompanyName = "ETH Zurich";
           this.CompanyName = "Microsoft"; //TODO: change this when a correct PLK has been obtained
           this.ProductVersion = "1.0";
           this.PackageLoadKeyId = 1;

           this.EditorFactoryType = typeof(EditorFactory);
           this.EditorName = "Zonnon Editor";

           this.ProjectFactoryType = typeof(ProjectFactory); //Guid is used in load key
           this.ProjectManager = typeof(ProjectManager);
           this.TemplateDirectory = Directory.GetParent(this.GetType().Module.Assembly.Location).Parent.Parent.FullName+"\\Templates\\";
        }

		[ComRegisterFunction]
		public static void RegisterFunction(Type t)
		{
			if (t != typeof(Registrar)) return;
			(new Registrar()).Register();
		}        
		
        [ComUnregisterFunction]
		public static void UnregisterFunction(Type t)
		{
			if (t != typeof(Registrar)) return;
			(new Registrar()).Unregister();
		}
	}
}
