# AltiumSharp
Reads and writes Altium library files, rendering them with C#. Many thanks to Tiago Trinidad (@Kronal on [Discord](https://discord.gg/MEQ5Xe5)) for his huge efforts on this library.

# Usage
You will need to add a NuGet references to your project for:
* OpenMcdf by ironfede
* System.Drawing.Common (even if it's a command line project)
* System.Text.Encoding.CodePages

## Opening a PcbLib File
    // open a PcbLib file.
    using (var reader = new PcbLibReader(fileName))
    {
	    // read the file
        reader.Read();
    
	    // iterate through each component in the library
        foreach (var component in reader.Components)
        {
            Console.WriteLine($"{component.Name}, {component.Pads}, {component.Primitives.Count()}");
        }
    
	    // Settings in the header.
        _displayUnit = reader.Header.DisplayUnit;
        _snapGridSize = reader.Header.SnapGridSize;
        _components = reader.Components.Cast<IComponent>().ToList();
    }

## Read/Write a SchLib File
	// open a SchLib file.
	string path = @"C:\altium-library\symbols\ARM Cortex";
            string[] files =
                { "SCH - ARM CORTEX - NXP LPC176X LQFP100.SCHLIB", "SCH - ARM CORTEX - NXP LPC176X TFBGA100.SCHLIB", "SCH - ARM CORTEX - NXP LPC176X WLCSP100.SCHLIB" };

            foreach (var file in files)
            {
                using (var reader = new AltiumSharp.SchLibReader())
                {
                    var schLib = reader.Read(Path.Combine(path, file));

                    // iterate through each component in the library
                    foreach (var component in schLib.Items)
                    {
                        Console.WriteLine($"{((IComponent)component).Name}, {component.Comment}");
                    }

                    // write schlib
                    using (var writer = new SchLibWriter())
                    {
                        writer.Write(schLib, Path.Combine(path, file), true);
                    }
                }
            }

## Rendering a Component

	_renderer = new PcbLibRenderer();
	
    if (_renderer != null)
    {
        _renderer.Component = _activeComponent;
        
        if (_pictureBoxImage == null) _pictureBoxImage = new Bitmap(pictureBox.Width, pictureBox.Height);
        using (var g = Graphics.FromImage(_pictureBoxImage))
        {
            _renderer.Render(g, pictureBox.Width, pictureBox.Height, _autoZoom, fastRendering);
        }
        _autoZoom = false;
    }
