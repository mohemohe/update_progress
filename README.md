update_progress
===============

### EXPERIMENTAL!!!!

最低限の例外処理しか入れていないのでエラーで落ちると思います。   
**あとで！あとでちゃんと直すから！！！**

----

update_progress で進捗を変更できます。  

#### 動作環境

.NET Framework 4.5 以上が動作するOS  
Wineでも動くと思います → [https://appdb.winehq.org/objectManager.php?sClass=version&iId=25478](https://appdb.winehq.org/objectManager.php?sClass=version&iId=25478)

#### 使用コマンド

|コマンド|効果|
|:----|:----|
|update_name [name]|名前を[name]に変更します|
|update_progress [value]|進捗を[value]に変更します|
|update_progress ++|進捗をインクリメントします|
|update_progress --|進捗をデクリメントします|
|update_name_progress [name]|進捗を保ったまま名前を[name]に変更します|

#### 使用例

時系列順です

@hogehuga update_name piyopiyo　→　名前が 'piyopiyo' に変更される  
@hogehuga update_progress 66.6　→　名前が 'piyopiyo: 66.6%' に変更される  
@hogehuga update_progress ++　→　名前が 'piyopiyo: 67.6%' に変更される  
@hogehuga update_progress --　→　名前が 'piyopiyo: 66.6%' に変更される  
@hogehuga update_name_progress ぴよぴよ　→　名前が 'ぴよぴよ: 66.6%' に変更される