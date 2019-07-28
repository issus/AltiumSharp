# AltiumSharp
Reads and writes Altium library files, rendering them with C#. Many thanks to Tiago Trinidad (@Kronal on [Discord](https://discord.gg/MEQ5Xe5)) for his huge efforts on this library.

# Usage

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

## Open a SchLib File
	// open a SchLib file.
	using (var reader = new SchLibReader(fileName))
    {
        reader.Read();
    
	    // iterate through each component in the library
        foreach (var component in reader.Components)
        {
            Console.WriteLine($"{component.Name}, {component.Description}");
        }
    
	    // settings in header.
        _displayUnit = reader.Header.DisplayUnit;
        _snapGridSize = reader.Header.SnapGridSize;
        _components = reader.Components.Cast<IComponent>().ToList();
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