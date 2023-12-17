using ExtractDx11MESH.MESHs;
using ExtractHelper;
using ExtractHelper.VariableTypes;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;


namespace ExtractDx11MESH;
public class ColladaExporter
{
    StreamWriter streamWriter;

    int totalNumberOfBones = 0;
    public List<string> boneNames;

    public void StartFile(string path)
    {
        streamWriter = new StreamWriter(path);

        string fileContents = @"<?xml version=""1.0"" encoding=""utf-8""?>
<COLLADA xmlns=""http://www.collada.org/2005/11/COLLADASchema"" version=""1.4.1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <asset>
    <contributor>
      <author>ExtractDx11Mesh</author>
      <authoring_tool>ExtractDx11Mesh</authoring_tool>
    </contributor>
    <unit name=""meter"" meter=""1""/>
    <up_axis>Y_UP</up_axis>
  </asset>
  <library_effects>
    <effect id=""Material-effect"">
      <profile_COMMON>
        <technique sid=""common"">
          <lambert>
            <emission>
              <color sid=""emission"">0 0 0 1</color>
            </emission>
            <diffuse>
              <color sid=""diffuse"">0.8 0.8 0.8 1</color>
            </diffuse>
            <index_of_refraction>
              <float sid=""ior"">1.45</float>
            </index_of_refraction>
          </lambert>
        </technique>
      </profile_COMMON>
    </effect>
  </library_effects>
  <library_images/>
  <library_materials>
    <material id=""Material-material"" name=""Material"">
      <instance_effect url=""#Material-effect""/>
    </material>
  </library_materials>
  <library_geometries>";

        streamWriter.Write(fileContents);
    }

    public void EndGeometriesAndStartSkin()
    {
		string fileContents = $@"
  </library_geometries>
  <library_controllers>";

		streamWriter.Write(fileContents);
	}

	public void EndSkinAndStartSceneForArmatures()
	{
        string fileContents = $@"
  </library_controllers>
  <library_visual_scenes>
    <visual_scene id=""Scene"" name=""Scene"">";
		streamWriter.Write(fileContents);

		if (this.totalNumberOfBones > 0)
        {
            fileContents = $@"<node id=""Armature"" name=""Armature"" type=""NODE"">";
            streamWriter.Write(fileContents);
        }

        AddAllBones();
	}

    private void AddAllBones()
    {
        AddBone(0);
    }

	/// <summary>
	///  Adds all bones recursively, one as a child of the other, from 0 to this.totalNumberOfBones.
    ///  This is done only so that the vertex weights can be exported properly. The bones are not
    ///  being parsed or laid out correctly.
    ///  Must be called with 0 as the starting value.
	/// </summary>
	/// <param name="boneNumber"></param>
	private void AddBone(int boneNumber)
    {
        if (boneNumber == 0 && boneNumber < this.totalNumberOfBones)
        {
            string boneName = getBoneName(boneNumber);
            string fileContents = $@"
        <node id=""Armature_Bone_{boneName}"" name=""{boneName}"" sid=""Bone_{boneName}"" type=""JOINT"">
          <matrix sid=""transform"">1 0 0 0 0 0 -1 0 0 1 0 0 0 0 0 1</matrix>";
            streamWriter.Write(fileContents);
			
            AddBone(boneNumber + 1);
			
            fileContents = $@"
              <extra>
                <technique profile=""blender"">
                  <layer sid=""layer"" type=""string"">0</layer>
                </technique>
              </extra>
            </node>";
			streamWriter.Write(fileContents);
		}
        else if (boneNumber < this.totalNumberOfBones)
        {
			string boneName = getBoneName(boneNumber);
			string fileContents = $@"
          <node id=""Armature_Bone_{boneName}"" name=""{boneName}"" sid=""Bone_{boneName}"" type=""JOINT"">
            <matrix sid=""transform"">1 0 0 0 0 1 0 1 0 0 1 0 0 0 0 1</matrix>";
			streamWriter.Write(fileContents);

			AddBone(boneNumber + 1);

			fileContents = $@"
            <extra>
              <technique profile=""blender"">
                <connect sid=""connect"" type=""bool"">1</connect>
                <layer sid=""layer"" type=""string"">0</layer>
              </technique>
            </extra>
          </node>";
			streamWriter.Write(fileContents);
		}
        else
        {
			string boneName = getBoneName(boneNumber);
			string fileContents = $@"
            <node id=""Armature_Bone_{boneName}"" name=""{boneName}"" sid=""Bone_{boneName}"" type=""JOINT"">
              <matrix sid=""transform"">1 0 0 0 0 1 0 1 0 0 1 0 0 0 0 1</matrix>
              <extra>
                <technique profile=""blender"">
                  <connect sid=""connect"" type=""bool"">1</connect>
                  <layer sid=""layer"" type=""string"">0</layer>
                  <tip_x sid=""tip_x"" type=""float"">0</tip_x>
                  <tip_y sid=""tip_y"" type=""float"">0</tip_y>
                  <tip_z sid=""tip_z"" type=""float"">1</tip_z>
                </technique>
              </extra>
            </node>";
			streamWriter.Write(fileContents);
		}
	}

    private string getBoneName(int index)
    {
        if (index >= 0 && index < boneNames.Count)
        {
            return boneNames[index];
        }
        else
        {
            return $"bone_{index + 1}";
        }
    }

	public void EndArmaturesAndStartSceneForObjects()
	{
		if (this.totalNumberOfBones > 0)
		{
			string fileContents = $@"
      </node>"; 
            streamWriter.Write(fileContents);
		}
	}

	public void AddScenesForObjects(MESH04 mesh, Part part, int partnumber)
	{
		VertexList vertexList = mesh.Vertexlistsdictionary[part.VertexListReferences1[0]];
		VertexList vertexList2 = null;
		if (part.VertexListReferences1.Count > 1)
		{
			vertexList2 = mesh.Vertexlistsdictionary[part.VertexListReferences1[1]];
		}
		List<int> list2 = null;
		try
		{
			list2 = mesh.Indexlistsdictionary[part.IndexListReference1];
		}
		catch (Exception ex)
		{
			ColoredConsole.WriteError("{0} @ Part {1:x4} Index {2:x4}", ex.Message, partnumber, part.IndexListReference1);
		}

		// Add scenes for objects with skins
		string meshNumber = $"{partnumber:0000}";
		if (vertexList.Vertices[0].BlendWeight0 == null && vertexList.Vertices[0].Position != null)
		{
			AddSceneForObject(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].BlendWeight0 == null && vertexList2.Vertices[0].Position != null)
		{
			AddSceneForObject(meshNumber, part, vertexList2);
		}
	}

	public void AddSceneForObject(string meshNumber, Part part, VertexList vertexList)
	{
		// For objects without bones
		string fileContents = $@"
        <node id=""Object.{meshNumber}"" sid=""Object.{meshNumber}"" name=""Object.{meshNumber}"">
          <instance_geometry url=""#Object_{meshNumber}-mesh"">
            <bind_material>
              <technique_common>
                <instance_material target=""#"" symbol="""" />
              </technique_common>
            </bind_material>
          </instance_geometry>
		</node>";
		streamWriter.Write(fileContents);
	}

	public void AddScenesForArmatures(MESH04 mesh, Part part, int partnumber)
	{
		VertexList vertexList = mesh.Vertexlistsdictionary[part.VertexListReferences1[0]];
		VertexList vertexList2 = null;
		if (part.VertexListReferences1.Count > 1)
		{
			vertexList2 = mesh.Vertexlistsdictionary[part.VertexListReferences1[1]];
		}
		List<int> list2 = null;
		try
		{
			list2 = mesh.Indexlistsdictionary[part.IndexListReference1];
		}
		catch (Exception ex)
		{
			ColoredConsole.WriteError("{0} @ Part {1:x4} Index {2:x4}", ex.Message, partnumber, part.IndexListReference1);
		}

		// Add scenes for objects with skins
		string meshNumber = $"{partnumber:0000}";
		if (vertexList.Vertices[0].BlendWeight0 != null)
		{
			AddSceneForArmature(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].BlendWeight0 != null)
		{
			AddSceneForArmature(meshNumber, part, vertexList2);
		}
	}

	public void AddSceneForArmature(string meshNumber, Part part, VertexList vertexList)
	{
        string rootBoneName = getBoneName(0);
        // For objects with bones
		string fileContents = $@"
        <node id=""Scene_{meshNumber}"" name=""Scene_{meshNumber}"" type=""NODE"">
          <matrix sid=""transform"">1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1</matrix>
          <instance_controller url=""#Armature_Object_{meshNumber}-skin"">
            <skeleton>#Armature_Bone_{rootBoneName}</skeleton>
          </instance_controller>
        </node>";
		streamWriter.Write(fileContents);
	}

	public void AddSkins(MESH04 mesh, Part part, int partnumber)
	{
		VertexList vertexList = mesh.Vertexlistsdictionary[part.VertexListReferences1[0]];
		VertexList vertexList2 = null;
		if (part.VertexListReferences1.Count > 1)
		{
			vertexList2 = mesh.Vertexlistsdictionary[part.VertexListReferences1[1]];
		}
		List<int> list2 = null;
		try
		{
			list2 = mesh.Indexlistsdictionary[part.IndexListReference1];
		}
		catch (Exception ex)
		{
			ColoredConsole.WriteError("{0} @ Part {1:x4} Index {2:x4}", ex.Message, partnumber, part.IndexListReference1);
		}

		// Add skins
		string meshNumber = $"{partnumber:0000}";
		if (vertexList.Vertices[0].BlendWeight0 != null)
		{
			AddSkin(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].Position != null)
		{
			AddSkin(meshNumber, part, vertexList2);
		}
	}

	public void AddSkin(string meshNumber, Part part, VertexList vertexList)
    {
        int maxBoneIndex = 0;
		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Byte4 indices = vertexList.Vertices[i].BlendIndices0;
            maxBoneIndex = Math.Max(maxBoneIndex, indices.a);
			maxBoneIndex = Math.Max(maxBoneIndex, indices.b);
			maxBoneIndex = Math.Max(maxBoneIndex, indices.c);
			maxBoneIndex = Math.Max(maxBoneIndex, indices.d);
		}
        int numberOfBones = maxBoneIndex + 1;

        if (numberOfBones > this.totalNumberOfBones)
        {
			this.totalNumberOfBones = numberOfBones;
		}

		string fileContents = @$"
    <controller id=""Armature_Object_{meshNumber}-skin"" name=""Armature"">
      <skin source=""#Object_{meshNumber}-mesh"">
        <bind_shape_matrix>1 0 0 0 0 1 0 0 0 0 1 1 0 0 0 1</bind_shape_matrix>
        <source id=""Armature_Object_{meshNumber}-skin-joints"">
          <Name_array id=""Armature_Object_{meshNumber}-skin-joints-array"" count=""{numberOfBones}"">";
		streamWriter.Write(fileContents);

		for (int i = 0; i < numberOfBones; i += 1)
		{
            string boneName = getBoneName(i);
			fileContents = $"Bone_{boneName} ";
			streamWriter.Write(fileContents);
		}

        fileContents = @$" </Name_array>
          <technique_common>
            <accessor source=""#Armature_Object_{meshNumber}-skin-joints-array"" count=""{numberOfBones}"" stride=""1"">
              <param name=""JOINT"" type=""name""/>
            </accessor>
          </technique_common>
        </source>
        <source id=""Armature_Object_{meshNumber}-skin-bind_poses"">
          <float_array id=""ArmatureObject_{meshNumber}-skin-bind_poses-array"" count=""{(numberOfBones) * 16}""> ";
		streamWriter.Write(fileContents);

		for (int i = 0; i < numberOfBones; i += 1)
		{
			fileContents = @$"1 0 0 0 0 0 1 0 0 -1 0 {-i} 0 0 0 1 ";
			streamWriter.Write(fileContents);
		}

        fileContents = @$" </float_array>
          <technique_common>
            <accessor source=""#Armature_Object_{meshNumber}-skin-bind_poses-array"" count=""{numberOfBones}"" stride=""16"">
              <param name=""TRANSFORM"" type=""float4x4""/>
            </accessor>
          </technique_common>
        </source>
        <source id=""Armature_Object_{meshNumber}-skin-weights"">
          <float_array id=""Armature_Object_{meshNumber}-skin-weights-array"" count=""{part.NumberVertices * 4}""> ";
		streamWriter.Write(fileContents);

		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Vector4 weights = vertexList.Vertices[i].BlendWeight0;
			fileContents = $"{(weights.X):0.000000} {(weights.Y):0.000000} {(weights.Z):0.000000} {(weights.W):0.000000} ".Replace(',', '.');
			streamWriter.Write(fileContents);
		}

		fileContents = @$" </float_array>
          <technique_common>
            <accessor source=""#Armature_Object_{meshNumber}-skin-weights-array"" count=""{part.NumberVertices * 4}"" stride=""1"">
              <param name=""WEIGHT"" type=""float""/>
            </accessor>
          </technique_common>
        </source>
        <joints>
          <input semantic=""JOINT"" source=""#Armature_Object_{meshNumber}-skin-joints""/>
          <input semantic=""INV_BIND_MATRIX"" source=""#Armature_Object_{meshNumber}-skin-bind_poses""/>
        </joints>
        <vertex_weights count=""{part.NumberVertices}"">
          <input semantic=""JOINT"" source=""#Armature_Object_{meshNumber}-skin-joints"" offset=""0""/>
          <input semantic=""WEIGHT"" source=""#Armature_Object_{meshNumber}-skin-weights"" offset=""1""/>
        <vcount>
        ";
		streamWriter.Write(fileContents);

		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Byte4 indices = vertexList.Vertices[i].BlendIndices0;
            int numberOfUniqueIndices = NumberOfUniqueIndices(indices);
			fileContents = $"{numberOfUniqueIndices} ";
			streamWriter.Write(fileContents);
		}

        fileContents = @$"
        </vcount>
        <v>
        ";
		streamWriter.Write(fileContents);

        int j = 0;
		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Byte4 indices = vertexList.Vertices[i].BlendIndices0;
			int numberOfUniqueIndices = NumberOfUniqueIndices(indices);
			if (numberOfUniqueIndices >= 1)
            {
				fileContents = $"{indices.a} {j} ";
				streamWriter.Write(fileContents);
			}
            if (numberOfUniqueIndices >= 2)
			{
				fileContents = $"{indices.b} {j + 1} ";
				streamWriter.Write(fileContents);
			}
			if (numberOfUniqueIndices >= 3)
			{
				fileContents = $"{indices.c} {j + 2} ";
				streamWriter.Write(fileContents);
			}
			if (numberOfUniqueIndices >= 4)
			{
				fileContents = $"{indices.d} {j + 3} ";
				streamWriter.Write(fileContents);
			}
			j += 4;
		}

		fileContents = @$"
        </v>
        </vertex_weights>
      </skin>
    </controller>";
		streamWriter.Write(fileContents);
	}

    public int NumberOfUniqueIndices(Byte4 BlendIndices0)
    {
        if (BlendIndices0.a == BlendIndices0.b)
        {
            return 1;
        }
		else if (BlendIndices0.a == BlendIndices0.c || BlendIndices0.b == BlendIndices0.c)
		{
			return 2;
		}
		else if (BlendIndices0.a == BlendIndices0.d || BlendIndices0.b == BlendIndices0.d || BlendIndices0.c == BlendIndices0.d)
		{
			return 3;
		}
        else
        {
            return 4;
        }
	}

	public void EndFile(int numberOfMeshes)
    {
        string fileContents = $@"
    </visual_scene>
  </library_visual_scenes>
  <scene>
    <instance_visual_scene url=""#Scene""/>
  </scene>
</COLLADA>";

        streamWriter.Write(fileContents);

        streamWriter.Close();
    }

    public void AddMesh(MESH04 mesh, Part part, int partnumber)
    {
        VertexList vertexList = mesh.Vertexlistsdictionary[part.VertexListReferences1[0]];
        VertexList vertexList2 = null;
        if (part.VertexListReferences1.Count > 1)
        {
            vertexList2 = mesh.Vertexlistsdictionary[part.VertexListReferences1[1]];
        }
        List<int> list2 = null;
        try
        {
            list2 = mesh.Indexlistsdictionary[part.IndexListReference1];
        }
        catch (Exception ex)
        {
            ColoredConsole.WriteError("{0} @ Part {1:x4} Index {2:x4}", ex.Message, partnumber, part.IndexListReference1);
        }

        // Add vertices
        string meshNumber = $"{partnumber:0000}";
        if (vertexList.Vertices[0].Position != null)
        {
            AddVertices(meshNumber, part, vertexList);
        }
        else if (vertexList2 != null && vertexList2.Vertices[0].Position != null)
        {
            AddVertices(meshNumber, part, vertexList2);
        }

        // Add UVs
        bool hasUV0s = true;
        if (vertexList.Vertices[0].UVSet0 != null)
        {
            hasUV0s = true;
			AddUV0s(meshNumber, part, vertexList);
        }
        else if (vertexList2 != null && vertexList2.Vertices[0].UVSet0 != null)
        {
			hasUV0s = true;
			AddUV0s(meshNumber, part, vertexList2);
        }

		bool hasUV1s = true;
		if (vertexList.Vertices[0].UVSet1 != null)
		{
			hasUV1s = true;
			AddUV1s(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].UVSet1 != null)
		{
			hasUV1s = true;
			AddUV1s(meshNumber, part, vertexList2);
		}

		// Add normals
		if (vertexList.Vertices[0].Normal != null)
        {
            AddNormals(meshNumber, part, vertexList);
        }
        else if (vertexList2 != null && vertexList2.Vertices[0].Normal != null)
        {
            AddNormals(meshNumber, part, vertexList2);
        }

        // Add colors
        bool hasColor = false;
		if (vertexList.Vertices[0].ColorSet0 != null)
		{
            hasColor = true;
			AddColors(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].ColorSet0 != null)
		{
			hasColor = true;
			AddColors(meshNumber, part, vertexList2);
		}

		AddFaces(meshNumber, part, list2, hasUV0s, hasUV1s, hasColor);
    }

    public void AddFaces(string meshNumber, Part part, List<int> indices, bool hasUV0s, bool hasUV1s, bool hasColor)
    {
        int numberOfFaces = part.NumberIndices / 3;

        string fileContents = $@"
        <vertices id=""Object_{meshNumber}-mesh-vertices"">
          <input semantic=""POSITION"" source=""#Object_{meshNumber}-mesh-positions""/>
        </vertices>
        <triangles material=""Material-material"" count=""{numberOfFaces}"">
          <input semantic=""VERTEX"" source=""#Object_{meshNumber}-mesh-vertices"" offset=""0""/>
          <input semantic=""NORMAL"" source=""#Object_{meshNumber}-mesh-normals"" offset=""1""/>";
		streamWriter.Write(fileContents);

        int numberOfAttributes = 2;
		if (hasUV0s)
        {
			fileContents = $@"
          <input semantic=""TEXCOORD"" source=""#Object-mesh-map-0"" offset=""{numberOfAttributes}"" set=""0""/>";
			streamWriter.Write(fileContents);
            numberOfAttributes++;
		}

		if (hasUV1s)
		{
			fileContents = $@"
          <input semantic=""TEXCOORD"" source=""#Object-mesh-map-1"" offset=""{numberOfAttributes}"" set=""0""/>";
			streamWriter.Write(fileContents);
			numberOfAttributes++;
		}

		if (hasColor)
		{
			fileContents = $@"
          <input semantic=""COLOR"" source=""#Object_{meshNumber}-mesh-colors"" offset=""{numberOfAttributes}"" set=""0""/>";
			streamWriter.Write(fileContents);
			numberOfAttributes++;
		}

		fileContents = $@"
          <p> ";
		streamWriter.Write(fileContents);

        for (int i = part.OffsetIndices; i < part.OffsetIndices + part.NumberIndices; i += 3)
        {
            string format;
            if (numberOfAttributes == 2)
            {
				format = "{0} {0}  {1} {1}  {2} {2} ";
			}
            else if (numberOfAttributes == 3)
			{
				format = "{0} {0} {0}  {1} {1} {1}  {2} {2} {2} ";
			}
            else if (numberOfAttributes == 4)
			{
				format = "{0} {0} {0} {0}  {1} {1} {1} {1}  {2} {2} {2} {2} ";
			}
			else
            {
				format = "{0} {0} {0} {0} {0}  {1} {1} {1} {1} {1}  {2} {2} {2} {2} {2} ";
			}

            int index1 = indices[i];
            int index2 = indices[i + 1];
            int index3 = indices[i + 2];

            fileContents = string.Format(format, index1, index2, index3);
            streamWriter.Write(fileContents);
		}

        fileContents = $@"</p>
        </triangles>
      </mesh>
    </geometry>";
        streamWriter.Write(fileContents);
    }

	public void AddColors(string meshNumber, Part part, VertexList vertexList)
	{
		string fileContents = @$"
        <source id=""Object_{meshNumber}-mesh-colors"">
          <float_array id=""Object_{meshNumber}-mesh-colors-array"" count=""{part.NumberVertices * 4}""> ";
		streamWriter.Write(fileContents);

		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Color4 color = vertexList.Vertices[i].ColorSet0;
            float red = ((float)color.R) / 255.0f;
			float green = ((float)color.G) / 255.0f;
			float blue = ((float)color.B) / 255.0f;
			float alpha = ((float)color.A) / 255.0f;

			fileContents = $"{red:0.000000} {green:0.000000} {blue:0.000000} {alpha:0.000000} ".Replace(',', '.');
			streamWriter.Write(fileContents);
		}

		fileContents = $@"</float_array>
          <technique_common>
            <accessor source=""#Object_{meshNumber}-mesh-colors-array"" count=""{part.NumberVertices}"" stride=""4"">
              <param name=""R"" type=""double"" />
              <param name=""G"" type=""double"" />
              <param name=""B"" type=""double"" />
              <param name=""A"" type=""double"" />
            </accessor>
          </technique_common>
        </source>";
		streamWriter.Write(fileContents);
	}

	public void AddNormals(string meshNumber, Part part, VertexList vertexList)
    {
        string fileContents = @$"
        <source id=""Object_{meshNumber}-mesh-normals"">
          <float_array id=""Object_{meshNumber}-mesh-normals-array"" count=""{part.NumberVertices * 3}""> ";
        streamWriter.Write(fileContents);

        for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
        {
            Vector3 normal = vertexList.Vertices[i].Normal;
            fileContents = $"{normal.X:0.000000} {normal.Y:0.000000} {normal.Z:0.000000} ".Replace(',', '.');
            streamWriter.Write(fileContents);
        }

        fileContents = $@"</float_array>
          <technique_common>
            <accessor source=""#Object_{meshNumber}-mesh-normals-array"" count=""{part.NumberVertices}"" stride=""3"">
              <param name=""X"" type=""float""/>
              <param name=""Y"" type=""float""/>
              <param name=""Z"" type=""float""/>
            </accessor>
          </technique_common>
        </source>";
        streamWriter.Write(fileContents);
    }

    public void AddUV0s(string meshNumber, Part part, VertexList vertexList)
    {
        string fileContents = @$"
        <source id=""Object-mesh-map-0"">
          <float_array id=""Object-mesh-map-0-array"" count=""{part.NumberVertices * 2}""> ";
        streamWriter.Write(fileContents);

        for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
        {
            Vector2 uVSet = vertexList.Vertices[i].UVSet0;
            fileContents = $"{uVSet.X:0.000000} {uVSet.Y:0.000000}\t".Replace(',', '.');
            streamWriter.Write(fileContents);
        }

        fileContents = $@"</float_array>
          <technique_common>
            <accessor source=""#Object-mesh-map-0-array"" count=""{part.NumberVertices}"" stride=""2"">
              <param name=""S"" type=""float""/>
              <param name=""T"" type=""float""/>
            </accessor>
          </technique_common>
        </source>";
        streamWriter.Write(fileContents);
    }

	public void AddUV1s(string meshNumber, Part part, VertexList vertexList) {
		string fileContents = @$"
        <source id=""Object-mesh-map-1"">
          <float_array id=""Object-mesh-map-1-array"" count=""{part.NumberVertices * 2}""> ";
		streamWriter.Write(fileContents);

		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Vector2 uVSet = vertexList.Vertices[i].UVSet1;
			fileContents = $"{uVSet.X:0.000000} {uVSet.Y:0.000000}\t".Replace(',', '.');
			streamWriter.Write(fileContents);
		}

		fileContents = $@"</float_array>
          <technique_common>
            <accessor source=""#Object-mesh-map-1-array"" count=""{part.NumberVertices}"" stride=""2"">
              <param name=""S"" type=""float""/>
              <param name=""T"" type=""float""/>
            </accessor>
          </technique_common>
        </source>";
		streamWriter.Write(fileContents);
	}

    public void AddVertices(string meshNumber, Part part, VertexList vertexList)
    {
        string fileContents = @$"
    <geometry id=""Object_{meshNumber}-mesh"" name=""Object.{meshNumber}"">
      <mesh>
        <source id=""Object_{meshNumber}-mesh-positions"">
          <float_array id=""Object_{meshNumber}-mesh-positions-array"" count=""{part.NumberVertices * 3}""> ";
        streamWriter.Write(fileContents);

        for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
        {
            Vector3 position = vertexList.Vertices[i].Position;
            fileContents = $"{position.X:0.000000} {position.Y:0.000000} {position.Z:0.000000} ".Replace(',', '.');
            streamWriter.Write(fileContents);
        }

        fileContents = @$"</float_array>
          <technique_common>
            <accessor source=""#Object_{meshNumber}-mesh-positions-array"" count=""{part.NumberVertices}"" stride=""3"">
              <param name=""X"" type=""float""/>
              <param name=""Y"" type=""float""/>
              <param name=""Z"" type=""float""/>
            </accessor>
          </technique_common>
        </source>";
        streamWriter.Write(fileContents);
    }

}

