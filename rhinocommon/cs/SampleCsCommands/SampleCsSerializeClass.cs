﻿using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.FileIO;
using Rhino.Input;

namespace SampleCsCommands
{
  [System.Runtime.InteropServices.Guid("43b12e34-84ea-4e27-b62b-1536b47b5698")]
  public class SampleCsSerializeClass : Command
  {
    public override string EnglishName
    {
      get { return "SampleCsSerializeClass"; }
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      ObjRef objref;
      var rc = RhinoGet.GetOneObject("Select object", false, ObjectType.AnyObject, out objref);
      if (rc != Result.Success)
        return rc;

      var obj = objref.Object();
      if (null == obj)
        return Result.Failure;

      var ud = obj.Attributes.UserData.Find(typeof(SampleCsCustomUserData)) as SampleCsCustomUserData;
      if (null == ud)
      {
        var rand = new Random();
        ud = new SampleCsCustomUserData
        {
          Data =
          {
            X = rand.NextDouble(),
            Y = rand.NextDouble(),
            Z = rand.NextDouble()
          }
        };
        obj.Attributes.UserData.Add(ud);
      }

      RhinoApp.WriteLine("{0} = {1}", ud.Description, ud.Data);

      return Result.Success;
    }
  }

  /// <summary>
  /// SampleCsCustomUserData
  /// </summary>
  [System.Runtime.InteropServices.Guid("23A7FA3D-74F6-4578-9059-B5E3611CD406")]
  public class SampleCsCustomUserData : UserData
  {
    public SampleCsCustomClass Data { get; set; }

    public SampleCsCustomUserData()
    {
      Data = new SampleCsCustomClass();
    }

    public bool IsValid
    {
      get { return Data.IsValid; }
    }

    public override string Description
    {
      get
      {
        return "SampleCsCustomClass";
      }
    }

    public override string ToString()
    {
      return Description;
    }

    protected override void OnDuplicate(Rhino.DocObjects.Custom.UserData source)
    {
      var src = source as SampleCsCustomUserData;
      if (null != src)
      {
        Data.Create(src.Data);
      }
    }

    public override bool ShouldWrite
    {
      get
      {
        return IsValid;
      }
    }

    protected override bool Read(BinaryArchiveReader archive)
    {
      int major, minor;
      archive.Read3dmChunkVersion(out major, out minor);
      if (1 == major && 0 == minor)
      {
        Data.Read(archive);
      }
      return !archive.ReadErrorOccured;
    }

    protected override bool Write(BinaryArchiveWriter archive)
    {
      archive.Write3dmChunkVersion(1, 0);
      Data.Write(archive);
      return !archive.WriteErrorOccured;
    }
  }

  /// <summary>
  /// SampleCsCustomClass
  /// </summary>
  [Serializable]
  public class SampleCsCustomClass
  {
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public SampleCsCustomClass()
    {
      X = Y = Z = 0.0;
    }

    public SampleCsCustomClass(SampleCsCustomClass src)
    {
      X = src.X;
      Y = src.Y;
      Z = src.Z;
    }

    public void Create(SampleCsCustomClass src)
    {
      X = src.X;
      Y = src.Y;
      Z = src.Z;
    }

    public bool IsValid
    {
      get
      {
        return RhinoMath.IsValidDouble(X) && 
               RhinoMath.IsValidDouble(Y) && 
               RhinoMath.IsValidDouble(Z);
      }
    }

    public bool Write(BinaryArchiveWriter archive)
    {
      var rc = false;
      if (null != archive)
      {
        try
        {
          // Write chunk version
          archive.Write3dmChunkVersion(1, 0);

          // Write 'this' object
          var formatter = new BinaryFormatter();
          var stream = new MemoryStream();
          formatter.Serialize(stream, this);
          stream.Seek(0, 0);
          var bytes = stream.ToArray();
          archive.WriteByteArray(bytes);
          stream.Close();

          // Verify writing
          rc = archive.WriteErrorOccured;
        }
        catch
        {
          // TODO
        }
      }
      return rc;
    }

    public bool Read(BinaryArchiveReader archive)
    {
      var rc = false;
      if (null != archive)
      {
        // Read and verify chunk version
        int major, minor;
        archive.Read3dmChunkVersion(out major, out minor);
        if (1 == major && 0 == minor)
        {
          try
          {
            // Read this object
            var bytes = archive.ReadByteArray();
            var stream = new MemoryStream(bytes);
            var formatter = new BinaryFormatter();
            var data = formatter.Deserialize(stream) as SampleCsCustomClass;
            Create(data);

            // Verify reading
            rc = archive.ReadErrorOccured;
          }
          catch
          {
            // TODO
          }
        }
      }
      return rc;
    }

    public override string ToString()
    {
      var culture = System.Globalization.CultureInfo.InvariantCulture;
      return string.Format("{0},{1},{2}", X.ToString(culture), Y.ToString(culture), Z.ToString(culture));
    }
  }

}
