# Hikariii
向osu!添加播放器以及Sayobot加速下载功能

## 安装方法
先将你在[Release](https://github.com/MATRIX-feather/LLin/releases)或[Actions](https://github.com/MATRIX-feather/LLin/actions/new)下载到的文件解压，然后：
1. 在游戏里点击`设置 ~> 打开 osu! 文件夹`
2. [将压缩包中的文件和文件夹安装到你的osu!游戏模式目录下](https://bbs.hiosu.com/thread-5-1-1.html)
4. 重启osu!
5. 完成！

## 食用指南
### 下载加速
点击任意未下载谱面的预览按钮（"`▶`"），待预览加载完毕后将会在左上角自动显示橙色的下载加速的选项。
下载按钮将在预览结束或下载完成后自动隐藏，你也可以通过点击左上角的按钮开手动关闭此弹窗。

### 播放器
播放器可以通过主界面和单人游戏选歌进入，按键可以在`输入 ~> 快捷键和键位绑定`中设置。

### Gosu集成
要使用gosu集成，首先前往gosu的[静态资源repo](https://github.com/l3lackShark/static/releases/)下载最新release的源码，然后解压到osu数据目录的`gosu_statics`中。

解压后的数据结构应类似于这样：
```
> gosu_statics
  > Classic
  > DarkAndWhite
  > ...
```

完成后重启游戏和录制/直播软件即可。
