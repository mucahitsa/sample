using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Text;
using Helper.Security;
using Helper.Extensions;
using Helper.Utility;

public class DataHandler : MonoBehaviour {

    public string extension;
    public string fileName;
    public static string ioPath { get => Path.Combine(Application.persistentDataPath, fileName + "." + extension); }

    /// <summary>saves file as binary in string(json) format (note that this method accepts Application.persistentDataPath as root)</summary>
    /// <param name="data">data to save</param>
    /// <param name="filePath">path of the file to load (accepts name of class + .extension as default if none given)</param>
    public static void SaveOrUpdate<T>(T data, string filePath = "") {
        string json = JsonConvert.SerializeObject(data);
        string fullPath = Path.Combine(Application.persistentDataPath, filePath == "" ? string.Concat(typeof(T).Name, ".", extension) : filePath);
        try {
            File.WriteAllBytes(fullPath, ToByteArray(json));
        } catch(Exception e) {
            Analytics.LogException(Utility.Concat(Constants.Analytics.SaveException , ": ", e.Message));
        }
    }

    /// <summary>loads binary file (note that this method accepts Application.persistentDataPath as root)</summary>
    /// <typeparam name="T">type of file</typeparam>
    /// <param name="filePath">path of the file to load (accepts name of class + .extension as default if none given)</param>
    /// <param name="getNewInstance">return new instance if loading failed (default null)</param>
    /// <returns>file</returns>
    public static T Load<T>(string filePath = "", bool getNewInstance = false) {
        var fullPath = Path.Combine(Application.persistentDataPath, filePath == "" ? string.Concat(typeof(T).Name, ".", extension) : filePath);
        try {
            if(File.Exists(fullPath)) {
                var bytes = File.ReadAllBytes(fullPath);
                T obj = Read<T>(ToString(bytes));
                return obj;
            } else {
                return getNewInstance ? (T)Activator.CreateInstance(typeof(T)) : default;
            }
        } catch(Exception e) {
            Debug.Log(e.Message);
            return getNewInstance ? (T)Activator.CreateInstance(typeof(T)) : default;
        }
    }

    /// <summary>Encrypts and saves file as binary in string(json) format (note that this method accepts Application.persistentDataPath as root)</summary>
    /// <param name="data">data to save</param>
    /// <param name="filePath">path of the file to load (accepts name of class + .extension as default if none given)</param>
    public static void SaveOrUpdateEncrypted<T>(string key, T data, string filePath = "") {
        string json = JsonConvert.SerializeObject(data);
        string fullPath = filePath.IfEmpty(ioPath);
        try {
            json = Cipher.Encrypt(json, key);
            File.WriteAllBytes(fullPath, ToByteArray(json));
        } catch(Exception e) {
            Analytics.LogException(Utility.Concat(Constants.Analytics.SaveException, ": ", e.Message));
        }
    }

    /// <summary>Loads and decyrpts binary file (note that this method accepts Application.persistentDataPath as root)</summary>
    /// <typeparam name="T">type of file</typeparam>
    /// <param name="filePath">path of the file to load (accepts name of class + .extension as default if none given)</param>
    /// <param name="getNewInstance">return new instance if loading failed (default null)</param>
    /// <returns>file</returns>
    public static T LoadDecrypted<T>(string key, string filePath = "", bool getNewInstance = false) {
        var fullPath = filePath.IfEmpty(ioPath);
        try {
            if(File.Exists(fullPath)) {
                var bytes = File.ReadAllBytes(fullPath);
                T obj = Read<T>(Cipher.Decrypt(ToString(bytes), key));
                return obj;
            } else {
                return getNewInstance ? (T)Activator.CreateInstance(typeof(T)) : default;
            }
        } catch(Exception e) {
            Debug.Log(e.Message);
            return getNewInstance ? (T)Activator.CreateInstance(typeof(T)) : default;
        }
    }

    public static void DeleteFile(string path) {
        try {
            if(File.Exists(path)) {
                File.Delete(path);
            } 
        } catch(Exception e) {
            Debug.Log(e.Message);
        }
    }

    public static T Read<T>(TextAsset textAsset) {
        return JsonConvert.DeserializeObject<T>(textAsset.text);
    }

    public static T Read<T>(string data) {
        return JsonConvert.DeserializeObject<T>(data);
    }

    public static byte[] ToByteArray(string s) {
        return Encoding.Default.GetBytes(s);
    }

    public static string ToString(byte[] ba) {
        string str = Encoding.Default.GetString(ba);
        return str;
    }
}