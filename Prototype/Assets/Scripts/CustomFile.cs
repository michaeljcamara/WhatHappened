using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomFile {

    private List<CustomType> types;
    private FileInfo _file;
    public FileInfo file { get { return _file; } }

    public string name { get { return _file.Name; } }

    public CustomFile(FileInfo file) {
        this._file = file;
    }

    public void SetTypesInFile(List<CustomType> types) {
        this.types = types;
    }
}
