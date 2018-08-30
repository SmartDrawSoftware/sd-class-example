# sd-class-example

This is a sample project meant to demonstrate the new SmartDraw class diagram extension, it is not intended for actual use.
It only works for public members and can only parse properties, fields, constants, enums, classes, nested classes and derived classes. Any more exotic C# features may result in undesierable results.

To use, choose an Assemby that has a .XML documentation file and pass the assemby and an output path into the JSONDocDump's CreateDumpFile method. If there is no documentation file, an exception will be thrown.

Here is an example of how to use it from a console application:

static void Main(string[] args)
{

     var dumper = new JSONDocDump();
     dumper.CreateDumpFile(Assembly.GetAssembly(typeof(JSONDocDump)), <output file path>);
}

This code produces the documentation of its own assembly if you included the xml documentation file in the same directory as the DLL.
