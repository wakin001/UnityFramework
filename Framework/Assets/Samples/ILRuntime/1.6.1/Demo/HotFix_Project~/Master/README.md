# cute_master

Masterデータ（.csv）ファイルを読み込んで読み込み用ScriptableObjectを生成する

先頭にプライマリーキーのidカラムが存在する場合は、SerializableDictionaryを使用します

SerializableDictionaryに外部ライブラリを使用しているため、cute_masterを使用する場合はゲーム内にライセンス表記が必要

https://github.com/neuecc/SerializableDictionary

## 使い方

最初にUnity内のメニューで「cute」>「ProjectPrefs」から[Master]タグがついた項目を設定

### ScriptableObjectクラス更新

プロジェクトの Tools/MasterSourceGenerator/make.bat を実行してください

csvの名前や型の情報が変わった場合はこの操作が必要です

### ScriptableObjectデータ更新

ビルド時にScriptableObjectに変換する必要があります

AssetBundleのビルド前に「Cute.Master.CreateMasterForBuild」を呼び出してください

csv追加時にcsvファイルを右クリック→「Cute」>「Master」>「Create master asset」を選択で、空のScriptableObjectが生成されます

このファイルはビルド時のアクセス用です

ファイルを生成しなくても動作しますが、guidが固定できないのでAssetBundleビルドした結果に差分が出る可能性があります

## csvファイルフォーマット

* 1行目：名前
* 2行目：型（int, int[], string, string[], float, float[], bool, bool[], プロジェクト定義enum, プロジェクト定義enum[]）
* 3行名以降：実データ

## 配列について

型名に[]を付与すると配列として定義できる

配列のデータは以下のようにカンマ区切りで定義する
ex) "1,2,3"

カンマ区切りのためstringの要素にカンマは使えない

## Csvのグループ化

csv名の末尾をアンダーバー＋数字にすることで、csvのグループ化ができます

また、この機能を使う場合は、Masterデータを読み直す際にMasterGroup.ResetAllを呼んでください

例）
master_weapon_001.csvがある場合、

MasterWeaponGroup.Instance.Get(1)

とすることでmaster_weapon_001.csvのデータを取得できる

## SerializableDictionaryについて

旧バージョンではDictionaryを初回アクセス時に生成してましたが、現バージョンではこの挙動はなくなりました

代わりにListへの初回アクセス時にわずかにallocationが発生します

なお、SerializableDictionaryを使用するとファイルサイズが増えます（当社比1.6倍）
