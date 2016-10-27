using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Converters;
using System.Reflection;
using Newtonsoft.Json;

namespace MyExtensions
{
   public static class MySerialize
   {
      private static JsonSerializerSettings defaultSettings = new JsonSerializerSettings() 
      { 
         ContractResolver = new MyContractResolver(),
         Formatting = Formatting.None,
         ObjectCreationHandling = ObjectCreationHandling.Replace
      };

      private static bool SaveObjectGeneral<T>(string filename, T saveObject, bool expanded = false)
      {
         try
         {
            using (StreamWriter file = File.CreateText(filename))
            {
               JsonSerializer serializer = new JsonSerializer();
               serializer.ContractResolver = defaultSettings.ContractResolver;
               serializer.ObjectCreationHandling = ObjectCreationHandling.Replace;

               if(expanded)
                  serializer.Formatting = Formatting.Indented;
               else
                  serializer.Formatting = Formatting.None;

               serializer.Serialize(file, saveObject);
            }
            //            string json = JsonConvert.SerializeObject(saveObject, defaultSettings); //expanded ? expandedSettings : defaultSettings);
            //            File.WriteAllText(filename, json);
         }
         catch (Exception e)
         {
            Console.WriteLine("STUPID SAVE EXCEPTION: " + e);
            return false;
         }

         return true;
      }

      public static bool SaveObjectReadable<T>(string filename, T saveObject)
      {
         return SaveObjectGeneral<T>(filename, saveObject, true);
      }

      //A quick and easy way to save objects to a file
      public static bool SaveObject<T>(string filename, T saveObject) where T : new()
      {
         return SaveObjectGeneral<T>(filename, saveObject, false);
      }

      //A quick and easy way to load an object from a file
      public static bool LoadObject<T>(string filename, out T loadObject) where T : new()
      {
         loadObject = new T();

         try
         {
            using (StreamReader file = File.OpenText(filename))
            {
               JsonSerializer serializer = new JsonSerializer();
               serializer.ContractResolver = defaultSettings.ContractResolver;
               serializer.ObjectCreationHandling = ObjectCreationHandling.Replace;

               loadObject = (T)serializer.Deserialize(file, typeof(T));
            }
//            string json = File.ReadAllText(filename);
//            loadObject = JsonConvert.DeserializeObject<T>(json, defaultSettings);
         }
         catch //(Exception e)
         {
            return false;
         }

         return true;
      }

      //Taken from http://stackoverflow.com/questions/24106986/json-net-force-serialization-of-all-private-fields-and-all-fields-in-sub-classe
      public class MyContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
      {
         protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
         {
            var props = /*type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
               .Select(p => base.CreateProperty(p, memberSerialization))
               .Union(*/type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                  .Select(f => base.CreateProperty(f, memberSerialization))//)
               .ToList();
            props.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props;
         }
      }
   }

   public static class MySerialize2
   {
      //This JSON serialization thing requires global settings.... great. These are the settings.
      private static readonly JsonSerializerSettings defaultSettings = new JsonSerializerSettings
      {
         ContractResolver = new MyContractResolver(),
         Formatting = Formatting.None,
         ObjectCreationHandling = ObjectCreationHandling.Replace,
         PreserveReferencesHandling = PreserveReferencesHandling.Objects
      };

      /// <summary>
      /// Save the given object as a JSON file with the given filename.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="filename"></param>
      /// <param name="saveObject"></param>
      /// <param name="expanded"></param>
      public static void SaveObject<T>(string filename, T saveObject, bool expanded = false)
      {
         JsonConvert.DefaultSettings = () => defaultSettings;

         using (StreamWriter filestream = File.CreateText(filename))
         {
            //IDK, maybe people don't want formatted json. Maybe they're crazy.
            var serializer = JsonSerializer.CreateDefault();
            serializer.Formatting = expanded ? Formatting.Indented : Formatting.None;
            serializer.Serialize(filestream, saveObject);
         }
      }

      /// <summary>
      /// Load an object from the given JSON file. 
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="filename"></param>
      public static T LoadObject<T>(string filename)
      {
         JsonConvert.DefaultSettings = () => defaultSettings;

         T newObject;

         using (StreamReader filestream = File.OpenText(filename))
         {
            var serializer = JsonSerializer.CreateDefault();
            newObject = (T)serializer.Deserialize(filestream, typeof(T));
         }

         return newObject;
      }

      //Taken from http//stackoverflow.com/questions/24106986/json-net-force-serialization-of-all-private-fields-And-all-fields-in-sub-classe
      public class MyContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
      {
         //This is just so that versions get automatically serialized correctly. Not sure why they're not already... honestly.
         protected override JsonContract CreateContract(Type objectType)
         {
            JsonContract contract = base.CreateContract(objectType);

            if (objectType == typeof(Version))
               contract.Converter = new VersionConverter();

            return contract;
         }

         protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
         {
            var currentType = type;
            List<JsonProperty> fields = new List<JsonProperty>();

            //Walk inheritance tree to get ALL values in ALL types. Yeah
            while (currentType != null)
            {
               fields.AddRange(currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | 
                        BindingFlags.Instance).Select(f => base.CreateProperty(f, memberSerialization)));
               currentType = currentType.BaseType;
            }

            var props = fields.ToList(); 

            props.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props;
         }
      }
   }
}
