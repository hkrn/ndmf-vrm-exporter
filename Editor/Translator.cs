// SPDX-FileCopyrightText: 2024-present hkrn
// SPDX-License-Identifier: MPL

#nullable enable
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf.localization;

// ReSharper disable once CheckNamespace
namespace com.github.hkrn
{
    internal static class Translator
    {
        internal static readonly Localizer Instance = new("en-us", () =>
        {
            var path = AssetDatabase.GUIDToAssetPath("9d1da1be88b74198955d08e38cddf4b4");
            var english = AssetDatabase.LoadAssetAtPath<LocalizationAsset>($"{path}/en-us.po");
            return new List<LocalizationAsset> { english };
        });

        public static string _(string key) => Instance.GetLocalizedString(key);
    }
}
