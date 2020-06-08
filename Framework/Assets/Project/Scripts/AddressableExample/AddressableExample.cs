using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableExample : MonoBehaviour
{
    [SerializeField]
    private AssetReference _asset = null;

    private GameObject _instance;

    private int _skinIndex = 1;

    private async void OnEnable()
    {
        // 加载会触发内部引用计数(ref count)增加1次，手动实例化不会变更引用计数，必须手动计数
        // Addressable没办法持有手动实例化后的物件的引用，Unload也不会摧毁对应实例

        #region Load by addressables and handle instantiate manually

        // AsyncOperationHandle 完成时的回调
        //_asset.LoadAssetAsync<GameObject>().Completed += OnLoadedCompleted;

        // 使用await/async 的加载方法
        // 开始加载，使用await 等待异步操作完成
        //var obj = await _asset.LoadAssetAsync<GameObject>().Task;
        //_instance = Instantiate(obj);

        #endregion

        // 使用Addressable直接实例化会增加引用计数，同时内部保存实例
        // 当使用ReleaseAsset或ReleaseInstance时，引用计数归零会自动Unload Bundle
        // 在不需要手动管理的情况下，推荐使用Addressable系统实例化

        #region Load and instantiate by addressables

        //_asset.InstantiateAsync().Completed += OnInstantiatedCompleted;

        // 使用await/async 的加载方法
        //_instance = await _asset.InstantiateAsync().Task;

        _instance = await Addressables.InstantiateAsync("Player").Task;

        Debug.Log("Instantiated finished");
        #endregion
    }

    private void OnInstantiatedCompleted(AsyncOperationHandle<GameObject> obj)
    {
        Debug.Log($"Instantiate {obj.Result.name} completed.");
        _instance = obj.Result;
    }

    private void OnLoadedCompleted(AsyncOperationHandle<GameObject> obj)
    {
        Debug.Log($"Load {obj.Result.name} from async operation.");
        _instance = Instantiate(obj.Result);
    }

    private void OnDisable()
    {
        // 使用Addressables实例化的实例不能手动删除，否则会破坏ref count的正确性
        // Ref Count到达0后会Unload bundle，这时如果依然引用到bundle内的资源的话，他们之间就会失去live-link，出现资源引用错误

        // 释放资源异步句柄
        // _asset.ReleaseAsset();

        // 释放实例，减少ref count，如果是Addressable来Instantiate的实例也会被删除
        // 切换场景时，场景上保有的物件如果是addressable生成的话，也会调用release instance
        // 如果此时ref count达到0也会触发bundle的unload（自动化管理）
        _asset.ReleaseInstance(_instance);
    }

    [ContextMenu("Change Texture")]
    public async void ChangeTexture()
    {
        _skinIndex = _skinIndex == 1 ? 2 : 1;
        var textures = await Addressables
            // 我们需要加载多个资源文件，同时因为贴图文件不需要实例化，所以这里使用LoadAssetsAsync
            // 这里由于增加了Label的选项，key我们得变成List<object>，同时值传入address和label
            .LoadAssetsAsync<Texture2D>(new List<object> { "Cube Texture", $"Skins{_skinIndex}" }
                // 这里的Callback与AsyncObjectHandle的Complete不一样，这里每加载一次就会调用一次回调
                // Addressables.MergeMode是关于资源的融合方式，一共有以下几种：
                // 假设我们根据key加载的资源分为[1,2,3][3,4,5]两组，那么
                // None和Use First都是返回第一组结果：[1,2,3]
                // Union返回组中满足任意一个key的结果：[1,2,3,4,5]
                // Intersection返回组中满足所有key的结果：[3]
                , null, Addressables.MergeMode.None).Task;
        // 将返回的贴图赋值给加载的材质
        _instance.GetComponent<Renderer>().material.mainTexture = textures[0];
    }
}
