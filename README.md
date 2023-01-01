
# AltiumSharp
AltiumSharp is a .NET library that makes it easy to read and write Altium Designer library files using C# without requiring Altium to be installed on your system. With AltiumSharp, you can programmatically access and modify the schematics, PCBs, components and symbols created in Altium, making it a powerful tool for automating tasks. This library was specifically created to make it easier to automate library management tasks.

Altium Designer is a leading electronic design automation software used for creating printed circuit board (PCB) designs. It stores information in a proprietary file format, which can be difficult to work with using traditional methods. AltiumSharp simplifies this process, allowing you to easily manipulate these files without the need for Altium Designer to be installed on your system.

In addition to supporting PcbLib and SchLib files, AltiumSharp also supports SchDoc and PcbDoc files. With AltiumSharp, you can easily read, write, and render Altium files in your .NET projects. Whether you're looking to automate library management tasks or simply need a more convenient way to work with Altium files, AltiumSharp has you covered.

Many thanks to Tiago Trinidad (@Kronal on [Discord](https://discord.gg/MEQ5Xe5)) for his huge efforts on this library.

# Usage
Before using AltiumSharp, you will need to add the following NuGet references to your project:
* [OpenMcdf](https://www.nuget.org/packages/OpenMcdf) by [ironfede](https://github.com/ironfede)
* [System.Text.Encoding.CodePages](https://www.nuget.org/packages/System.Text.Encoding.CodePages)

Once you have these references added, you can use the AltiumSharp library to read and write Altium library files and render components as images. Check out the examples in the readme for more information on how to use AltiumSharp in your projects.

## Opening a PcbLib File
Here's an example of how you can use AltiumSharp to open a PcbLib file and iterate through its components:
```csharp
	// Open a PcbLib file.
	using (var reader = new PcbLibReader(fileName))
	{
	    // Read the file.
	    reader.Read();

	    // Iterate through each component in the library.
	    foreach (var component in reader.Components)
	    {
	        // Print information about the component.
	        Console.WriteLine($"Name: {component.Name}");
	        Console.WriteLine($"Number of Pads: {component.Pads}");
	        Console.WriteLine($"Number of Primitives: {component.Primitives.Count()}");
	    }

	    // Retrieve settings from the header.
	    _displayUnit = reader.Header.DisplayUnit;
	    _snapGridSize = reader.Header.SnapGridSize;
	    _components = reader.Components.Cast<IComponent>().ToList();
	}
  ```

## Reading and Writing a SchLib File
AltiumSharp also provides support for reading and writing SchLib files, as shown in this example:
```csharp
	// Set the path to the directory containing the SchLib files.
	string path = @"C:\altium-library\symbols\ARM Cortex";

	// Array of filenames to process.
	string[] files = {
	    "SCH - ARM CORTEX - NXP LPC176X LQFP100.SCHLIB",
	    "SCH - ARM CORTEX - NXP LPC176X TFBGA100.SCHLIB",
	    "SCH - ARM CORTEX - NXP LPC176X WLCSP100.SCHLIB"
	};

	// Loop through each file.
	foreach (var file in files)
	{
	    // Read the SchLib file.
	    using (var reader = new AltiumSharp.SchLibReader())
	    {
	        var schLib = reader.Read(Path.Combine(path, file));

	        // Iterate through each component in the library.
	        foreach (var component in schLib.Items)
	        {
	            // Print the component name and comment.
	            Console.WriteLine($"Name: {((IComponent)component).Name}");
	            Console.WriteLine($"Comment: {component.Comment}");
	        }

	        // Write the SchLib file.
	        using (var writer = new SchLibWriter())
	        {
	            writer.Write(schLib, Path.Combine(path, file), true);
	        }
	    }
	}
```

## Rendering a Component
AltiumSharp includes a renderer for PcbLib files that allows you to display components in your own application. Here's an example of how you can use the renderer to render a component:
```csharp
	// Create a PcbLibRenderer.
	var renderer = new PcbLibRenderer();

	// Check if the renderer was created successfully.
	if (renderer != null)
	{
	    // Set the component to be rendered.
	    renderer.Component = _activeComponent;

	    // Create a Bitmap to hold the rendered image.
	    var image = new Bitmap(pictureBox.Width, pictureBox.Height);

	    // Use a Graphics object to draw on the Bitmap.
	    using (var g = Graphics.FromImage(image))
	    {
	        // Render the component to the Bitmap.
	        renderer.Render(g, pictureBox.Width, pictureBox.Height, _autoZoom, fastRendering);
	    }

	    // Update the picture box with the rendered image.
	    pictureBox.Image = image;

	    // Reset the auto zoom flag.
	    _autoZoom = false;
	}
```