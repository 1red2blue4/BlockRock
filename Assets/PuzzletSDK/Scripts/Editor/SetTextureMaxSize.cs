using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SetTextureMaxSize {

	private static int AudioCompressionBitrate = 128000;

	[MenuItem("Edit/DecreaseWebTextures")]
	public static void DecreaseWebTextureSize () {
		List<string> texturePaths = GetAllTexturePaths();
		foreach(string path in texturePaths){
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
			Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
			int newMaxSize = Mathf.Max(texture.width, texture.height) / 2;
			importer.SetPlatformTextureSettings("Web", newMaxSize, importer.textureFormat);
			AssetDatabase.ImportAsset(path);
		}
	}

	/*[MenuItem("Edit/AdjustCompressionRate")]
	public static void AdjustCompression () {
		List<string> audioPaths = GetAllPaths("t:AudioClip");
		foreach(string path in audioPaths){
			AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(path);
			Debug.Log(string.Format("{0}: bits per second {1}", path, importer.compressionBitrate));
			if(importer.format == AudioImporterFormat.Compressed && importer.compressionBitrate != AudioCompressionBitrate){
				importer.compressionBitrate = AudioCompressionBitrate;
			} else {
				importer.format = AudioImporterFormat.Compressed;
				importer.loadType = AudioClipLoadType.DecompressOnLoad;
				importer.compressionBitrate = AudioCompressionBitrate;
			}
			AssetDatabase.ImportAsset(path);
		}
	}*/

	private static List<string> GetAllTexturePaths () {
		string[] guids = AssetDatabase.FindAssets("t:Texture2D");
		List<string> paths = new List<string>(guids.Length);
		foreach(string guid in guids){
			paths.Add(AssetDatabase.GUIDToAssetPath(guid));
		}
		return paths;
	}

	private static List<string> GetAllPaths(string searchTerms){
		string[] guids = AssetDatabase.FindAssets(searchTerms);
		List<string> paths = new List<string>(guids.Length);
		foreach(string guid in guids){
			paths.Add(AssetDatabase.GUIDToAssetPath(guid));
		}
		return paths;
	}
}
