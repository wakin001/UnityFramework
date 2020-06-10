using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Framework;

namespace HotFix_Project
{
    /// <summary>
    /// 共通なメソッド
    /// </summary>
    public class CommonUtil
    {
        /// <summary>
        /// ImageのColorModulusを取得
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static float GetColorModulus(Material imageMaterial)
        {
            if (imageMaterial.HasProperty("_ColorModulus"))
            {
                return imageMaterial.GetFloat("_ColorModulus");
            }
            return 1.0f;
        }

        /// <summary>
        /// 発光するImageを設定する
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="ev"></param>
        public static void SetColorModulusForAtlasMaterial(Image image, float ev, bool isAtlas)
        {
            if (image == null || image.material == null)
            {
                return;
            }

            if (image.material.HasProperty("_ColorModulus"))
            {
                image.material.SetFloat("_ColorModulus", ev);
            }
            else
            {
                Material assetMat;
                if (isAtlas)
                {
                    assetMat = (Material)LoadMaterialOnCache("Materials/Common/SpriteMaskableExternalAlphaAdjustment");
                }
                else
                {
                    // 中身はすでにMaskable
                    assetMat = (Material)LoadMaterialOnCache("Materials/Common/SpriteExternalColorModulusAdjustment");
                }
                if (assetMat != null)
                {
                    var mat = new Material(assetMat);
                    mat.mainTexture = image.material.mainTexture;

                    if (mat.HasProperty("_AlphaTex") && image.material.HasProperty("_AlphaTex"))
                    {
                        if (image.material.HasProperty("_AlphaTex"))
                        {
                            mat.SetTexture("_AlphaTex", image.material.GetTexture("_AlphaTex"));
                        }
                    }

                    mat.SetFloat("_ColorModulus", ev);
                    image.material = mat;
                }

            }
        }

        /// <summary>マテリアルを可能であればキャッシュから読み込む</summary>
        public static Material LoadMaterialOnCache(string path)
        {
            Material mat;

            //if (OutGameAssetLoader.IsInstanceEmpty())
            {
                mat = (Material)AssetManager.Load("Materials/Common/UnlitTransparentSeparateAlphaAdjustment");
            }
            //else
            //{
            //    mat = (Material)OutGameAssetLoader.instance.GetResidentAssets(path);
            //}

            return mat;
        }

        /// <summary>
        /// 改行コードの整形
        /// </summary>
        public static string FormatNewLineCode(string str)
        {
            if (str == null)
            {
                return "";
            }
            str = str.Replace("\\n", System.Environment.NewLine);
            return str.Replace("\n", System.Environment.NewLine);
        }
    }
}
