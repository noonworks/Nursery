# Nursery

![Nursery](img/nursery.png)

## 概要

Discordのテキストチャンネルに投稿された文章を[棒読みちゃん](http://chi.usamimi.info/Program/Application/BouyomiChan/)で読み上げ、その音声をボイスチャンネルに流すbotプログラムです。

[![Nursery 使用動画](http://img.youtube.com/vi/ZkET1jFBRDA/0.jpg)](http://www.youtube.com/watch?v=ZkET1jFBRDA)


## インストール方法、使い方

[github上のwiki](https://github.com/noonworks/Nursery/wiki)を参照してください。


## 特徴

### 参加者全員に聞こえる読み上げbot

一般的な読み上げソフトは、音声をPCのスピーカーに流します。そのため、読み上げ音声はソフトを起動した人にだけ聞こえます。読み上げ音声を他の人に共有するには、ボイスチャンネルとは別の配信などを利用する必要があります。

Nurseryは、読み上げた音声をDiscordのボイスチャンネルに流します。そのため、 **ボイスチャンネルに参加している全員に同じ音声が聞こえます。** スマートフォンなどでDiscordを使っている参加者も読み上げ音声を聞くことができます。 

また、Nurseryはbotアカウントを介して読み上げ音声を流すため、読み上げが不要だと思ったら、botをミュートすれば対応できます。

### どんな用途に向いているか

Nurseryは以下のような状況に適しています：

* 読み上げ音声を参加者全員で聞きたいとき
* スマートフォンなど、読み上げソフトが実行できない環境の人にも読み上げ音声を共有したいとき

以下のような状況ならば、他の読み上げソフトの方が適しているかもしれません：

* 読み上げ音声が自分にだけ聞こえればいい場合
* 読み上げ音声を別の方法で配信する場合（動画配信など）

### 棒読みちゃんのコマンドを活用可能

棒読みちゃんを「配信者向け」に設定することで、棒読みちゃんの機能の一部をそのまま使用することができます。以下はその一例です。

* `教育(言葉=読み方)`コマンドを使用することで、読み方を登録することができます。
* `エコー)`コマンドを使用することで、読み上げ音声にエコーをかけることができます。

制限は以下の通りです：

* `Sound`：`Sound`コマンドの音声は使用者のPCでしか聞こえません。
（代わりに、NurseryのSEプラグインを使用することで、ボイスチャンネル上で音声を再生することができます。）

### 仮想サウンドデバイスを使用

仮想サウンドデバイスとは、大雑把に言えば「マイクとスピーカーのフリをするソフトウェア」です。

Nurseryではまず、棒読みちゃんの音声を「スピーカーのフリ」をした仮想サウンドデバイスに流すよう設定します。そして、同じ仮想サウンドデバイスに「マイクのフリ」をさせることで、読み上げ音声をDiscordのボイスチャンネルに流します。


## ライセンスおよび免責事項

[LICENSE](LICENSE)を参照してください。


## 営利目的での利用について

[Nurseryのライセンス](LICENSE)は商用利用および営利目的での利用を妨げるものではありません。

しかし、後述の権利表記にある通り、同梱の[BASS audio library](https://www.un4seen.com/)および[BASS.NET](http://bass.radio42.com/)は非営利目的での利用に限り無償提供されています。そのため、Nurseryを営利目的で使用することはできません。

Nurseryのソースコードそのものは、[LICENSE](LICENSE)に従い、営利目的でも利用することができます。


## 権利表記

### ライブラリ

* [BASS audio library - un4seen.com](https://www.un4seen.com/)
* [BASS.NET - radio42](http://bass.radio42.com/)
* [BouyomiChanClient.cs - 棒読みちゃん](http://chi.usamimi.info/Program/Application/BouyomiChan/)
* [CommandLineParser](https://github.com/commandlineparser/commandline) ([MIT License](https://github.com/commandlineparser/commandline/blob/master/License.md))
* [Discord.Net](https://github.com/RogueException/Discord.Net) ([MIT License](https://github.com/RogueException/Discord.Net/blob/dev/LICENSE))
* [Jint](https://github.com/sebastienros/jint) ([BSD 2-Clause "Simplified" License](https://github.com/sebastienros/jint/blob/dev/LICENSE.txt))
* [libsodium](https://download.libsodium.org/doc/) ([ISC license](https://en.wikipedia.org/wiki/ISC_license))
* [Newtonsoft Json.NET](https://www.newtonsoft.com/json) ([MIT License](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md))
* [NGettext](https://github.com/neris/NGettext) ([MIT License](https://github.com/neris/NGettext/blob/master/LICENSE))
* [opas.dll - Opus Interactive Audio Codec](http://opus-codec.org/) ([three-clause BSD license](http://opus-codec.org/license/))

### ロゴ、アイコン画像素材

* [いらすとや](https://www.irasutoya.com/)
  * [黄色い通学帽のイラスト（ハット）](https://www.irasutoya.com/2018/06/blog-post_275.html)
  * [ゲーマーの男の子のイラスト（将来の夢）](https://www.irasutoya.com/2018/04/blog-post_405.html)
* [Fontfabric](https://www.fontfabric.com/)
  * [Uni Sans Free](https://www.fontfabric.com/uni-sans-free/)

### 効果音

* [無料効果音で遊ぼう！](http://taira-komori.jpn.org/)
  * [拍手短め２](http://taira-komori.jpn.org/event01.html)


## ロゴ、アイコン

Nurseryへのリンク、紹介などに使用してください。

![Nursery](img/nursery.png)
![Nursery](img/nursery256.png)
![Nursery](img/nursery48.png)
![Nursery](img/nursery32.png)
![Nursery](img/nursery16.png)
![Nursery](img/nursery_text_white.png)
![Nursery](img/nursery_text_blue.png)
