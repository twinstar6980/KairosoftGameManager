# Kairosoft Game Manager

开罗游戏管理器。

> 适用于通过 Steam 安装的 Windows 版本开罗游戏。

[更新日志](./CHANGELOG.md)

## 应用预览

![manager](./media/preview/manager.png)

![function](./media/preview/function.png)

![setting](./media/preview/setting.png)

## 功能列表

* 禁用游戏对存档文件的加密与验证。

* 启用游戏的调试模式（仅部分游戏有效）。

* 加密或解密游戏存档。

* 导出或导入游戏存档（允许跨设备、跨账号转移存档）。

## 使用方法

1. 在 [Release](https://github.com/twinstar6980/KairosoftGameManager/releases/tag/Latest) 页中下载并安装最新版本的 `msix` 安装包文件。
	
	> 安装前需要先信任 MSIX 中的签名证书。\
	> 右键查看 `.msix` 的属性，切换到 ⌈ 数字签名 ⌋ 页，选择列表中第一项，再点击 ⌈ 详细信息 ⌋ ，在弹出的窗口中依次选择 ⌈ 查看证书 ⌋ - ⌈ 安装证书 ⌋ - ⌈ 本地计算机 ⌋ - ⌈ 将所有证书都放入下列存储 ⌋ - ⌈ 受信任人 ⌋ ，完成证书的安装。

	> GitHub Release 中仅保留最后一次分发，历史分发可以在我的 [OneDrive](https://1drv.ms/f/c/2d321feb9cd374ed/Eu1005zrHzIggC2GAAAAAAABZClnjoZtr_WdR-EfZLTLkA?e=JZRzDV) 中找到。

2. 启动应用，应用会检索并列出当前 Steam 账号库中已安装至本地的开罗游戏，点击列表项右下角的的火焰图标会弹出功能选项，选择并等待应用完成工作即可。
	
	> 如果你的 Steam 没有安装至默认路径，需要在应用的设置页中的 `Repository Directory` 一栏填写 Steam 的安装路径。\
	> 如果需要应用对游戏程序进行自动修改，需要下载 [Il2CppDumper v6.7.40 x86](https://github.com/Perfare/Il2CppDumper) ，并在应用的设置页中的 `Program File Of Il2CppDumper` 一栏填写 `Il2CppDumper-x86.exe` 的文件路径。

## 数据安全

为了获取已安装的开罗游戏与存档密钥，应用会检索本地 Steam 账号的本地库状态信息；应用不会在本地或网络上存储这些信息，项目中也不存在与联网访问相关的代码与权限。

尽管经过了一定的测试，但应用 **无法保证** 在修改游戏程序或存档时不会造成数据丢失的严重后果；应用不对丢失的数据负责，如有疑虑，请在操作之前自行备份游戏的存档文件（位于 ⌈ Steam ⌋ - ⌈ 库 ⌋ - ⌈ 游戏页 ⌋ - ⌈ 设置项 ⌋ - ⌈ 管理 ⌋ - ⌈ 浏览本地文件 ⌋ - ⌈ saves 目录 ⌋ ）。

## 开源说明

本项目可以在不违反 **GPL v3** 的基础上自由地使用与修改。

本项目不为因使用本项目代码或程序而产生的任何问题负责。
