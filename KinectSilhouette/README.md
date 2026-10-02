# 森と人影：Azure Kinect / Unity

Unity 6000.3.21f1で開き、Assets/Scenes/Projection.unity を開いて再生します。
Unity Hubの「追加」→ディスク上のプロジェクトを追加から、このKinectSilhouetteフォルダーを選びます。
すぐ試す場合は Build/KinectSilhouette.exe をダブルクリックしてください。
先に Azure Kinect Viewer と Body Tracking Viewer を終了してください。
人の領域だけを黒く描き、森の写真に重ねます。録画・音声取得・外部送信は行いません。

## 操作
- M：左右反転
- 矢印：人影の位置
- + / -：人影の大きさ
- R：位置・大きさ・反転のリセット
- H：操作説明を隠す／表示
- F：全画面切替、Esc：全画面解除

UnityのInspectorで Silhouette Screen の色、位置、大きさ、GPU Device Idを変更できます。
変更は再生停止後に設定し、シーンを保存すると保持されます。
背景は Assets/Resources/forest.jpg を別の画像に差し替えられます。

## 投影
PCとAladdin MarcaをHDMIで接続。Windowsの表示設定を「拡張」にし、
Build/KinectSilhouette.exeを起動してウィンドウをプロジェクター側へ移動、Fで全画面にします。
Hで操作説明を消します。人影の縦横比は保持されます。

## 背景写真
Hugo Delauney / Unsplash
https://unsplash.com/photos/landscape-photography-of-forest-0O4C9qtn_48
Unsplash License: https://unsplash.com/license
取得画像は背景としてプロジェクトに使用しています。

## SDK
このPCにインストール済みのSensor SDK 1.4.2 / Body Tracking SDK 1.1.2を利用。
SDKファイルのライセンスは各インストール先のLICENSE/REDIST等に従ってください。
Unity向けのSDKライブラリは Assets/Plugins、認識モデルは Assets/StreamingAssets にコピー済み。
Kinect本体のネイティブDLLを実行ファイルの横に重複配置すると別々に読み込まれ、
取得ハンドルが認識側で無効になり得るため、KinectのDLLはPlugins/x86_64だけに置きます。
背景の明るさはInspectorの Background Lighten（初期値0.2）で調整できます。
DirectML方式、深度NFOV Unbinned、30fps入力。実効認識速度はPC環境によります。
Windows 11 24H2の実機動作およびプロジェクターとの遅延は現場で確認が必要です。

## 確認結果（2026-09-29）
Unity 6000.3.21f1でコンパイルおよびWindows x64ビルド成功。
Direct3D 11 + DirectMLで、接続されたKinectから25秒テスト中に636フレームの認識結果を受信。
検出人数は0人。実際の人影の輪郭、左右反転、プロジェクターでの表示は未確認。
