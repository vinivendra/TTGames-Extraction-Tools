using System.Text;
using ExtractDx11MESH.MESHs;
using ExtractHelper;

namespace ExtractDx11MESH;

public class GSC2EB
{
	protected byte[] fileData;

	protected int iPos;

	public int version;

	public GSC2EB(byte[] fileData, int iPos)
	{
		this.fileData = fileData;
		this.iPos = iPos;
		version = BigEndianBitConverter.ToInt32(fileData, iPos);
		this.iPos += 4;
		ColoredConsole.WriteLineInfo("{0:x8} GSC2 Version 0x{1:x2}", iPos, version);
	}

	public int Read(ref int referencecounter, string directoryName, string filenameWithoutExtension)
	{
		int num = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}   Number of resources: 0x{1:x8}", iPos, num);
		iPos += 4;
		for (int i = 0; i < num; i++)
		{
			iPos += 3;
			int numberofchars = BigEndianBitConverter.ToInt16(fileData, iPos);
			iPos += 2;
			ColoredConsole.WriteLine("{0:x8}   {1}", iPos, readString(numberofchars));
			numberofchars = BigEndianBitConverter.ToInt16(fileData, iPos);
			iPos += 2;
			ColoredConsole.WriteLine("{0:x8}     {1}", iPos, readString(numberofchars));
		}
		iPos += 4;

		// Create the mesh
		MESH04 mesh = new MESHC9(fileData, iPos);
		mesh.Read(ref referencecounter);

		// Export the mesh to a file
        ColoredConsole.WriteLine("Exporting Collada file...");

        ColladaExporter colladaExporter = new ColladaExporter();
        string path = directoryName + "\\" + filenameWithoutExtension + ".dae";
        colladaExporter.StartFile(path);

        foreach (Part part in mesh.Parts)
        {
            num++;
            colladaExporter.AddMesh(mesh, part, num);
        }

        colladaExporter.EndFile(mesh.Parts.Count);

		// Return the new reading position in the input file
        return iPos;
	}

	protected string readString(int numberofchars)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < numberofchars; i++)
		{
			if (fileData[iPos] != 0)
			{
				stringBuilder.Append((char)fileData[iPos]);
			}
			iPos++;
		}
		return stringBuilder.ToString();
	}
}
