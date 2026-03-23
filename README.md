# CgLogViewer

BlueCG のチャットログを見やすく表示する Windows デスクトップアプリです。  
リアルタイム監視を中心に、ログ閲覧、翻訳、Discord 通知、お食事タイマー、NPC 会話抽出まで 1 つのツールで扱えます。

![CgLogViewer のメイン画面](docs/logViewer.png)

## 主な機能

- リアルタイム監視
  `Log` フォルダを監視し、新着ログをリアルタイムで表示します。
- ログ閲覧モード
  任意のログファイルを選んで、あとから内容を確認できます。
- 重複ログの自動統合
  近い時刻に流れた同じ本文のログは 1 件にまとめて表示します。
- カテゴリ分類とフィルター
  `通常 / パーティ / ギルド / NPC / システム / その他` に分類して絞り込めます。
- シンプルビュー
  軽量な別ウィンドウでログだけを表示できます。
- 翻訳
  `DeepL API Free`、`Google Cloud Translation`、`OpenAI API` を使ってログを翻訳できます。
- Discord 通知
  条件に一致したログを Discord Webhook に送信できます。
- お食事タイマー
  料理ログを検出して、キャラごとの 3 分クールダウンを表示します。
- NPC 会話抽出
  対象プロセスから NPC 会話を読み取り、翻訳やメインビュー追加ができます。

## ログ表示

- 文字コードは `Big5` 前提で読み込みます。
- URL を含むログはクリックしてブラウザで開けます。
- ログはカード表示で、カテゴリごとに色分けされます。
- 表示中のログは Unicode テキストとして保存できます。

## 翻訳

- 各ログカードから `翻訳 / 原文` を切り替えられます。
- OpenAI を選んだ場合は `reasoning_effort` を設定できます。
- DeepL はローカル辞書に加えて、用語集アップロードにも対応しています。
- Google と OpenAI はローカル辞書を使って固有名詞を保護します。

## 翻訳辞書

- 初期辞書はソースに含まれる [translation_dictionary.tsv](/C:/Users/seven/CgLogListener/src/CgLogViewer/translation_dictionary.tsv) です。
- 実行時の編集内容は `translation_dictionary.user.tsv` に保存されます。
  初回起動時に、初期辞書から自動で作成されます。
- 設定画面の `辞書を編集...` から専用画面を開いて編集できます。

## NPC 会話抽出

- `NPC会話抽出` から対象の `bluecg.exe` プロセスを選択できます。
- 現在表示中の会話をリアルタイムで確認できます。
- `翻訳`、`コピー`、`ログに追加` が使えます。
- `自動的にログに追加する` をオンにすると、会話更新時に自動でメインビューへ追加します。

## 設定

設定画面では次の内容を変更できます。

- ゲームフォルダ
- リアルタイム表示件数
- 同一ログのまとめ秒数
- 通知音と SE 音量
- 標準通知ルール
- カスタムキーワード通知
- Discord Webhook URL
- 翻訳プロバイダと API キー
- OpenAI `reasoning_effort`

## セキュリティ

- API キーは `settings.ini` に暗号化して保存されます。
- 既存の平文設定も読み込めるため、古い設定ファイルからそのまま移行できます。
- 次回保存時に暗号化形式へ更新されます。

## 動作環境

- Windows
- `.NET Framework 4.6`
- BlueCG のログフォルダへアクセスできること

## ビルド

Visual Studio で [CgLogViewer.csproj](/C:/Users/seven/CgLogListener/src/CgLogViewer/CgLogViewer.csproj) を開いて `Debug` または `Release` でビルドしてください。

出力先:

- `Debug`: [bin/Debug/CgLogViewer.exe](/C:/Users/seven/CgLogListener/src/CgLogViewer/bin/Debug/CgLogViewer.exe)
- `Release`: [bin/Release/CgLogViewer.exe](/C:/Users/seven/CgLogListener/src/CgLogViewer/bin/Release/CgLogViewer.exe)

## 配布物

配布用 ZIP は [artifacts/release](/C:/Users/seven/CgLogListener/artifacts/release) に出力します。
