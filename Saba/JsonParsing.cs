using System.Numerics;
using System.Text.Json.Nodes;
using Saba.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Saba;

#region Enums

public enum JsonShadowType : byte
{
    Off,
    Mode1,
    Mode2,
}

#endregion

#region Classes

public class JsonHeader(JObject jsonReader)
{
    public string Title { get; } = jsonReader["Header"]["FileSignature"].ToString();

    public string ModelName { get; } = jsonReader["Header"]["ModelName"].ToString();
}

public class JsonMotion(JArray jArray, int count)
{
    public string BoneName { get; } = ((JObject)jArray[count])["Name"].ToString();

    public uint Frame { get; } = ((JObject)jArray[count])["FrameNo"].Value<uint>();

    public Vector3 Translate { get; } =
        new Vector3(JArray.Parse(jArray[count]["Location"].ToString())[0].Value<float>(),
            JArray.Parse(jArray[count]["Location"].ToString())[1].Value<float>(),
            JArray.Parse(jArray[count]["Location"].ToString())[2].Value<float>());


    public Quaternion Quaternion { get; } = new Quaternion(
        -JArray.Parse(jArray[count]["Rotation"]["Quaternion"].ToString())[2].Value<float>(),
        -(JArray.Parse(jArray[count]["Rotation"]["Quaternion"].ToString())[3].Value<float>()),
        -JArray.Parse(jArray[count]["Rotation"]["Quaternion"].ToString())[1].Value<float>(),
        -JArray.Parse(jArray[count]["Rotation"]["Quaternion"].ToString())[0].Value<float>());

    public JObject Interpolation { get; } = (JObject)jArray[count]["Interpolation"];
}

public class JsonMorph(JArray jArray, int count)
{
    public string BlendShapeName { get; } = jArray[count]["Name"].ToString();

    public uint Frame { get; } = jArray[count]["FrameNo"].Value<uint>();

    public float Weight { get; } = jArray[count]["Weight"].Value<float>();
}

public class JsonCamera(JArray jArray, int count)
{
    public uint Frame { get; } = jArray[count]["FrameNo"].Value<uint>();

    public float Distance { get; } = jArray[count]["Length"].Value<float>();

    public Vector3 Interest { get; } = new Vector3();

    public Vector3 Rotate { get; } = new Vector3();

    public byte[] Interpolation { get; } = new byte[24];

    public uint ViewAngle { get; } = 0;

    public bool IsPerspective { get; } = true;
}

public class JsonLight()
{
    public uint Frame { get; } = 0;

    public Vector3 Color { get; } = new Vector3();
    public Vector3 Position { get; } = new Vector3();
}

public class JsonShadow()
{
    public uint Frame { get; } = 0;

    public ShadowType Mode { get; } = ShadowType.Off;

    public float Distance { get; } = 0;
}

public class JsonIk
{
    public class Info(JArray jArray, int index)
    {
        public string Name { get; } = jArray[index]["BoneName"].Value<string>();


        public bool Enable { get; } = jArray[index]["Enabled"].Value<bool>();
    }

    public uint Frame { get; }

    public bool Show { get; }

    public Info[] Infos { get; }

    public JsonIk(JArray jArray, int index)
    {
        Frame = jArray[index]["FrameNo"].Value<uint>();
        Show = jArray[index]["Visible"].Value<bool>();
        JArray data = JArray.Parse(jArray[index]["Data"].ToString());

        Infos = new Info[data.Count];

        for (int i = 0; i < Infos.Length; i++)
        {
            Infos[i] = new Info(data, i);
        }
    }
}

#endregion

public class JsonParsing
{
    public JsonHeader Header { get; }

    public JsonMotion[] Motions { get; }

    public JsonMorph[] Morphs { get; }


    public JsonIk[] Iks { get; }

    internal JsonParsing(JsonHeader header,
        JsonMotion[] motions,
        JsonMorph[] morphs,
        JsonIk[] iks)
    {
        Header = header;
        Motions = motions;
        Morphs = morphs;
        Iks = iks;
    }

    public static JsonParsing? ParsingByFile(string path)
    {
        JObject jsonReader = JObject.Parse(File.ReadAllText(path));

        JsonHeader header = ReadHeader(jsonReader);

        if (header.Title != "Vocaloid Motion Data 0002" && header.Title != "Vocaloid Motion Data")
        {
            return null;
        }

        return new JsonParsing(header,
            ReadMotions(jsonReader),
            ReadMorphs(jsonReader),
            ReadIks(jsonReader));
    }

    private static JsonHeader ReadHeader(JObject jsonReader)
    {
        return new JsonHeader(jsonReader);
    }

    private static JsonMotion[] ReadMotions(JObject jsonReader)
    {
        JsonMotion[] motions =
            new JsonMotion[jsonReader["Motion"]["Count"].Value<int>()];
        JArray jArray = JArray.Parse(jsonReader["Motion"]["Data"].ToString());

        for (int i = 0; i < motions.Length; i++)
        {
            motions[i] = new JsonMotion(jArray, i);
        }

        return motions;
    }

    private static JsonMorph[] ReadMorphs(JObject jsonReader)
    {
        JsonMorph[] morphs =
            new JsonMorph[jsonReader["Skin"]["Count"].Value<int>()];
        JArray jArray = JArray.Parse(jsonReader["Skin"]["Data"].ToString());

        for (int i = 0; i < morphs.Length; i++)
        {
            morphs[i] = new JsonMorph(jArray, i);
        }

        return morphs;
    }

    private static JsonCamera[] ReadCameras(JObject jsonReader)
    {
        JsonCamera[] cameras =
            new JsonCamera[jsonReader["Camera"]["Count"].Value<int>()];
        JArray jArray = JArray.Parse(jsonReader["Camera"]["Data"].ToString());

        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i] = new JsonCamera(jArray, i);
        }

        return cameras;
    }

    private static JsonLight[] ReadLights(JObject jsonReader)
    {
        JsonLight[] lights =
            new JsonLight[jsonReader["Skin"]["Count"].Value<int>()];

        for (int i = 0; i < lights.Length; i++)
        {
            lights[i] = new JsonLight();
        }

        return lights;
    }

    private static JsonShadow[] ReadShadows(JObject jsonReader)
    {
        return null;
    }

    private static JsonIk[] ReadIks(JObject jsonReader)
    {
        JsonIk[] iks = new JsonIk[jsonReader["IK"]["Count"].Value<int>()];
        JArray jArray = JArray.Parse(jsonReader["IK"]["Data"].ToString());
        for (int i = 0; i < iks.Length; i++)
        {
            iks[i] = new JsonIk(jArray, i);
        }

        return iks;
    }
}