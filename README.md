# UuIiFrameWork

UuIiViewを使ったMVP/MVVMハイブリッドUIフレームワーク

## 概要

UuIiFrameWorkは、Unity向けのUI管理フレームワークです。MVPパターンをベースに、リアクティブなデータバインディング機能を提供します。

## 動作環境

- Unity 2021.3 以上
- 依存パッケージ
  - UuIiView
  - Newtonsoft.Json

## インストール

Package Managerから以下のURLを追加：
```
https://github.com/sayken/UuIiFrameWork.git
```

## ディレクトリ構造

```
UuIiFrameWork/
├── Runtime/
│   ├── Interface/          # コアインターフェース
│   ├── UIPanel/            # パネルシステム
│   └── Utils/              # ユーティリティ
└── Editor/                 # エディタ拡張
```

## アーキテクチャ

```
[UIView] → [UIPanel] → [Presenter] → [Router] → [Model/ViewModel]
              ↑                                        ↓
              └──────── データバインディング ───────────┘
```

## 主要コンポーネント

### UILayer（シングルトン）

UI表示階層の管理を行います。

```csharp
// 初期化
UILayer.Inst.Initialize(uiPanelData);

// パネルの追加
var panel = UILayer.Inst.AddPanel("MainPanel");
panel.Open(data);

// パネルのクローズ
UILayer.Inst.Close("MainPanel");

// レイヤー単位でクローズ
UILayer.Inst.CloseByLayer("Popup", "Dialog");
```

### UIPanel

UI画面の基本クラスです。

```csharp
public class MyPanel : UIPanel
{
    public override void OnOpen()
    {
        // パネルが開いた時の処理
    }

    public override void OnClose()
    {
        // パネルが閉じる時の処理
    }
}
```

### UIPresenter

パネルのロジックを管理するPresenterです。

```csharp
public class MyPresenter : UIPresenter
{
    public MyPresenter(Router router, string panelName, Model model)
        : base(router, panelName, model) { }

    public override void OnEvent(CommandLink cmd)
    {
        base.OnEvent(cmd);

        switch (cmd.ActionType)
        {
            case ActionType.Action:
                HandleAction(cmd);
                break;
        }
    }

    private void HandleAction(CommandLink cmd)
    {
        // カスタムアクション処理
    }
}
```

### ReactivePresenter

リアクティブなデータバインディング機能を持つPresenterです。

```csharp
public class MyReactivePresenter : ReactivePresenter
{
    public MyReactivePresenter(Router router, string panelName, Model model)
        : base(router, panelName, model) { }

    protected override void OnPanelOpen()
    {
        // JSONからViewModelを初期化
        string json = @"{""title"": ""Hello"", ""count"": 0}";
        VM.Init(json);
    }

    public override void OnEvent(CommandLink cmd)
    {
        base.OnEvent(cmd);

        if (cmd.EventName == "IncrementButton")
        {
            var count = (int)VM.Get("count");
            VM.UpdateData("count", count + 1, forceNotify: true);
        }
    }
}
```

### ViewModel

JSONデータの管理とデータバインディングを提供します。

```csharp
// 初期化
VM.Init(@"{""name"": ""Player1"", ""score"": 100}");

// データ取得
var name = VM.Get("name");

// データ更新（即時通知）
VM.UpdateData("score", 200, forceNotify: true);

// リスト内のデータ更新
VM.UpdateListData("items", "item_001", "selected", "true", forceNotify: true);
```

### Router

イベントのルーティングを管理します。

```csharp
// Presenterの登録
UILayer.Inst.Router.SetPresenter("MainPanel", typeof(MainPresenter), model);

// グループPresenterの登録
UILayer.Inst.Router.SetGroupPresenter(typeof(MyGroupPresenter), uiGroup, model);
```

### CommandLink

イベント情報を構造化するクラスです。

フォーマット: `PanelName/EventType/ActionType/EventName/ParentName/Id[/param=value...]`

```csharp
// コマンドリンクの作成
var cmd = CommandLink.CreateOpen(ePanelName.SettingsPanel);

// パラメータ付き
var cmd = new CommandLink("MainPanel/Button/Open/SettingsBtn/Parent/001/mode=dark");
Console.WriteLine(cmd.param["mode"]); // "dark"
```

### Model

型ベースのモデル管理を行います。

```csharp
// モデルの登録
var model = new Model();
model.Add(new PlayerModel());
model.Add(new GameSettingsModel());

// モデルの取得
var player = model.Get<PlayerModel>();
```

## EventType / ActionType

### EventType
| 値 | 説明 |
|---|---|
| Button | ボタンクリック |
| Toggle | トグル変更 |
| Slider | スライダー変更 |
| Input | テキスト入力 |
| LongTap | 長押し |
| DragAndDrop | ドラッグ&ドロップ |

### ActionType
| 値 | 説明 |
|---|---|
| Open | パネルを開く |
| Close | パネルを閉じる |
| CloseAndOpen | 現在のパネルを閉じて別パネルを開く |
| Action | カスタムアクション |
| DataSync | データ同期 |
| ActionToPanel | 特定パネルへのアクション |

## エディタツール

### UIPanelData Settings

メニュー: `Tools > UuIiView > UIPanelData Settings`

- パネルプレハブの一覧管理
- レイヤー設定
- ブラインド設定
- Enum自動生成

## 設定ファイル

### UIPanelData（ScriptableObject）

パネル情報の設定ファイルです。

1. `Assets > Create > UuIiView > UIPanelData` で作成
2. CanvasRootプレハブを設定
3. UIプレハブのパスを指定
4. エディタウィンドウで詳細設定

### BlindType

| 値 | 説明 |
|---|---|
| None | ブラインドなし |
| NoAction | タップ無効 |
| Close | タップでパネルを閉じる |
| Custom | カスタム処理 |

## SafeArea対応

`SafeAreaSupport`コンポーネントをCanvasに追加することで、ノッチ対応が自動的に行われます。

## ライセンス

MIT License

## 更新履歴

### v0.1.0
- 初回リリース
- MVPパターン実装
- ReactivePresenter追加
- エディタツール追加
