using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(StringListStringDictionary))]
public class DictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer { }

[CustomPropertyDrawer(typeof(StringListStorage))] 
public class SictionaryStoragePropertyDrawer :SerializableDictionaryStoragePropertyDrawer { }