﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("MPExtended.Services.UserSessionService")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyProduct("MPExtended UserSessionService")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("9b7805f6-464e-423d-ac19-98a6db9fac75")]

// The hosting information
[assembly: MPExtended.Libraries.Service.Hosting.ServiceAssembly(
    Service = MPExtended.Libraries.Service.MPExtendedService.UserSessionService,
    WCFType = typeof(MPExtended.Services.UserSessionService.UserSessionProxyService),
    ZeroconfType = "_mpextended-uss._tcp.")]