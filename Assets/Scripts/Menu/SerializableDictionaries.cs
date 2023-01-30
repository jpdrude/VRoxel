using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class StringListStorage : SerializableDictionary.Storage<List<string>> { }

[Serializable]
public class StringListStringDictionary : SerializableDictionary<string, List<string>, StringListStorage> { }