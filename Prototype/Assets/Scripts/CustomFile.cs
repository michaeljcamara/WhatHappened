using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomFile {

    private List<CustomType> types;
    private FileInfo _info;
    public FileInfo info { get { return _info; } }

    public string name { get { return _info.Name; } }

    public CustomFile(FileInfo file) {
        this._info = file;
    }

    public void SetTypesInFile(List<CustomType> types) {
        this.types = types;
    }
}
