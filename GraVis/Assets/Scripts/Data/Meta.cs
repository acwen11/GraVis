using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class Meta
{
    public Dictionary<string, PropertyMeta> Properties;
    public List<string> PropertyNames;
    public DataHandler dataHandler;

    public string rootDirectory;

    public Meta(string parentDirectory, DataHandler dataHandler)
    {
        Properties = new Dictionary<string, PropertyMeta>();
        PropertyNames = new List<string>();
        this.rootDirectory = parentDirectory;
        LoadMainMetafile();
        this.dataHandler = dataHandler;
    }

    public void AddLoadingFinishedListener(string propertyName, Action method)
    {
        Properties[propertyName].FinishedLoadingEvent += method;
    }

    public Dictionary<string, PropertyMeta> GetProperties()
    {
        return Properties;
    }

    public PropertyMeta GetProperty(string propertyName)
    {
        return Properties[propertyName];
    }

    public void ProcessProperties()
    {
        /*
        foreach (var property in Properties)
        {
            property.Value.Process();
        }
        */
        Properties["Rho"].Process();
        Properties["B"].Process();
    }

    public void LoadTimeStep(int timestep)
    {
        // Later, we want to load all active properties
        Properties["B"].SwitchTimeStep(timestep);
        Properties["Rho"].SwitchTimeStep(timestep);
    }


    /// <summary>
    /// Checks if all active datasets are done loading and if so, returns the current loaded token (timestep)
    /// </summary>
    /// <param name="loadingToken">Returns the current token. If data is not ready, discard the output!</param>
    /// <returns></returns>
    public bool AllDatasetsReady(out int loadingToken)
    {
        // Later, we need to test this against all properties
        loadingToken = Properties["B"].GetLoadingToken();
        return Properties["B"].IsCurrentTokenLoaded();// && Properties["Rho"].IsCurrentTokenLoaded();
    }

    public void LoadMainMetafile()
    {
        // first 4 bytes:   int     - Enum of metadata type
        // 4 bytes:         int     - Number of dataset properties
        // 4 bytes:         int     - Length of property name
        // n bytes:         string  - utf-8 encoded string of property name (n bytes)
        // 4*4 bytes:       intx4     - dimension of dataset [x,y,z space dim, w sample dim)

        int byteIndex = 0;
        byte[] data = File.ReadAllBytes(rootDirectory+"/metaData.meta");
        var buffer = new ReadOnlySpan<byte>(data);
        int type = BitConverter.ToInt32(buffer[..4]);
        int numberOfProperties = BitConverter.ToInt32(buffer.Slice(4,4));
        byteIndex = 8;

        for (int i = 0; i < numberOfProperties; i++)
        {
            int bytesOfString = BitConverter.ToInt32(buffer.Slice(byteIndex, 4));
            byteIndex += 4;
            string propName = System.Text.Encoding.UTF8.GetString(buffer.Slice(byteIndex, bytesOfString));
            byteIndex += bytesOfString;
            Vector3Int spaceDim = new Vector3Int(
                BitConverter.ToInt32(buffer.Slice(byteIndex, 4)),
                BitConverter.ToInt32(buffer.Slice(byteIndex + 4, 4)) ,
                BitConverter.ToInt32(buffer.Slice(byteIndex + 8, 4)) );
            int sampleDim = BitConverter.ToInt32(buffer.Slice(byteIndex + 12, 4));
            byteIndex += 16;
            Properties.Add(propName, new PropertyMeta(propName, spaceDim, sampleDim, this));
            PropertyNames.Add(propName);
        }
    }

    public void Unload()
    {
        foreach(var property in Properties)
        {
            property.Value.Unload();
        }
    }
    
}
