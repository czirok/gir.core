<img src="https://raw.githubusercontent.com/gircore/gir.core/main/img/logo.svg" align="right" />

# Welcome to Gir.Core

[![Continuous integration status](https://github.com/GirCore/gir.core/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/gircore/gir.core/actions)
[![Website depolyment status](https://github.com/GirCore/gircore.github.io/actions/workflows/deploy.yml/badge.svg?branch=develop)](https://github.com/gircore/gircore.github.io/actions)

Gir.Core provides C# bindings for several [GObject] based libraries like [GTK] for user interfaces.

This project aims to provide a complete set of APIs for writing rich cross-platform user interfaces and multimedia programs. It is built upon the well-established [GObject Introspection][gi] framework for language bindings.

## Features
* **Idiomatic C#:** An API which feels natural to C# developers (including the async/await feature).
* **Simplicity:** Memory management is handled automatically, greatly simplifying the C-API.
* **Complete API:** Support for the entire GTK and GStreamer stack, enabling feature-rich applications which deeply integrate with the OS.
* **Extensibility:** Allows 3rd party developers to write bindings for other GObject-based libraries, achieving full interoperability between them.

## Status
The code is under active development and certain features may not be available. The API is subject to change as long as version 1.0 is not released. There are nuget packages available. Feel free to visit the [nuget organization][GirCoreNuget] to get an overview.

In general, you can expect the following features to be available:
- Creation of native structs / records.
- Creation of native `GObject` based classes.
- Creation of native enumerations.
- Usage of native `GObject` based interfaces.
- Reading / writing properties of native `GObject` based classes.
- Receiving signals of native `GObject` based classes.
- Retaining state of purely managed `GObject.Object` based subclasses.

Missing features of the generator:
- Overriding of native virtual methods.
- Custom implementations of native `GObject` interfaces.
- Fundamental types like `GObject.Paramspec` or `Gtk.Expression` are only available in parts.

Please be aware that the list of unsupported features should not be considered complete as certain corner cases may not yet be listed. If such a case is encountered a bug report is welcome.

The upcoming features and releases can be checked out in the [milestones](https://github.com/gircore/gir.core/milestones).

## Generated Libraries

| Library                                   | Description                                            |
|-------------------------------------------|--------------------------------------------------------|
| [GTK-4.0][Gtk4Nuget]                      | UI-Toolkit                                             |
| [Libadwaita-1][LibadwaitaNuget]           | Building blocks for modern GNOME applications          |
| [GStreamer-1.0][GstNuget]                 | Multimedia Framework                                   |
| [Cairo-1.0][CairoNuget]                   | Graphics Library                                       |
| [Pango-1.0][PangoNuget]                   | Font/Text Library                                      |
| [Gio-2.0][GioNuget]                       | Library for high level application functionality       |
| [GdkPixbuf-2.0][GdkPixbufNuget]           | Image loading in various formats                       |
| [WebKit-6.0][WebKitNuget]                 | Browser engine (Linux only)                            |
| [JavaScriptCore-6.0][JavaScriptCoreNuget] | JavaScript engine for WebKit (Linux only)              |
| [GtkSource-5][GtkSourceNuget]             | Extends a Gtk.TextView to be like a source code editor |
| [Secret-1][SecretNuget]                   | Access to the system keyring via libsecret             |
| [Rsvg-2.0][RsvgNuget]                     | SVG rendering library                                  |


## Get Involved
Anyone who wants to help is very welcome. If you want to get in contact feel free to chat with us via matrix ([#gircore:matrix.org](https://matrix.to/#/#gircore:matrix.org?via=matrix.org)) or open a [discussion](https://github.com/gircore/gir.core/discussions) and don't forget to check out our [contribution guidelines](docs/docs/contributing.md).

## Build
To generate the bindings locally execute the following commands in a terminal. Make sure to initialise submodules with `--recursive` otherwise the `gir-files` directory will not be loaded properly.

```sh
$ git clone --recursive https://github.com/gircore/gir.core.git
$ cd gir.core/scripts
$ dotnet fsi GenerateLibs.fsx
$ cd ../src
$ dotnet build GirCore.Libs.slnf
```

If you want to clean the [Libs folder](src/Libs) of all generated files run in the scripts folder:

    $ dotnet fsi CleanLibs.fsx

For more advanced build options, see the [documentation](docs/docs/build.md).

## Code structure
The folder structure in this repository is organized as follows:
* **[src/Generation/GirTool](src/Generation/GirTool):** The tool to generate the bindings.
* **[src/Generation/GirLoader](src/Generation/GirLoader):** A library for reading and resolving GObject Introspection repositories.
* **[src/Generation/GirModel](src/Generation/GirModel):** An interface based definition of the GObject data model. Used by the loader and generator to have a common understanding of the GObject data model.
* **[src/Generation/Generator](src/Generation/Generator):** Code generator generates C# code from GObject Introspection data.
* **[src/Libs](src/Libs):** Contains manually written code for libraries. The generator outputs code here.
* **[src/Samples](src/Samples):** Example programs using GTK, GStreamer, and others.
* **[src/Extensions](src/Extensions):** Auxilary libraries which extend the core libraries.
* **[src/Tests](src/Tests):** Unit and Integration tests.
* **[ext/gir-files](https://github.com/gircore/gir-files):** Introspection data from [gircore/gir-files](https://github.com/gircore/gir-files).

The code in the library folder is not complete because most of the code is generated when the [GirTool](src/Generation/GirTool) is run.

[gi]: https://gi.readthedocs.io/
[gstreamer]: https://gstreamer.freedesktop.org/
[GIO]: https://developer.gnome.org/gio/stable/
[GObject]: https://developer.gnome.org/gobject/stable/
[GTK]: https://gtk.org/
[libhandy]: https://source.puri.sm/Librem5/libhandy/
[WebKitGTK]: https://webkitgtk.org/
[JavaScriptCore]: https://webkitgtk.org/reference/jsc-glib/stable/index.html
[dbus]: https://www.freedesktop.org/wiki/Software/dbus/
[libchamplain]: https://wiki.gnome.org/Projects/libchamplain/
[GtkSharp]: https://github.com/GtkSharp/GtkSharp/
[GdkPixbuf]: https://gitlab.gnome.org/GNOME/gdk-pixbuf/
[GirCoreNuget]: https://www.nuget.org/profiles/GirCore/
[Gtk4Nuget]: https://www.nuget.org/packages/GirCore.Gtk-4.0/
[GstNuget]: https://www.nuget.org/packages/GirCore.Gst-1.0/
[CairoNuget]: https://www.nuget.org/packages/GirCore.Cairo-1.0/
[PangoNuget]: https://www.nuget.org/packages/GirCore.Pango-1.0/
[GioNuget]: https://www.nuget.org/packages/GirCore.Gio-2.0/
[GdkPixbufNuget]: https://www.nuget.org/packages/GirCore.GdkPixbuf-2.0/
[LibadwaitaNuget]: https://www.nuget.org/packages/GirCore.Adw-1/
[WebKitNuget]: https://www.nuget.org/packages/GirCore.WebKit-6.0/
[JavaScriptCoreNuget]: https://www.nuget.org/packages/GirCore.JavaScriptCore-6.0/
[GtkSourceNuget]: https://www.nuget.org/packages/GirCore.GtkSource-5/
[SecretNuget]: https://www.nuget.org/packages/GirCore.Secret-1/
[RsvgNuget]: https://www.nuget.org/packages/GirCore.Rsvg-2.0/

## Licensing terms
Gir.Core is licensed under the terms of the MIT-License. Please see the [license file](license.txt) for further information.

The [Gir.Core logo](img/logo.svg) is built upon the [original GTK logo](https://wiki.gnome.org/Projects/GTK/Logo) by Andreas Nilsson which is licensed under the [GNU Free Documentation License](https://www.gnu.org/licenses/fdl-1.3.txt) and was relicensed under CC BY-SA 3.0. Therefore the Gir.Core logo is licensed under the [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/deed.en), too.

## Special Thanks
Special thanks go to the people and ogranizations who make this project possible:
- All [contributors](https://github.com/gircore/gir.core/graphs/contributors) for donating their time in form of code, feedback and bug reports.
- [JetBrains](https://www.jetbrains.com/) for providing open source licenses of their products.
- [GitHub](https://github.com) for providing an open source friendly code hosting platform.
