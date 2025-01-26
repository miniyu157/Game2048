# 2048 Game

这是由 WPF 小白制作的第二个 WPF 程序。

## 概述

- 在 2048 经典玩法基础上添加了 **到达临界值时扩容** 的玩法
- 自动游玩的算法是根据当前局势和预判下一步的局势进行综合评估
- 总分目前根据全部格子的总和减去 6 计算
- 配置文件保存在 ```\Game2048.dat```

> 项目依赖 [EleCho.WpfSuite](https://github.com/OrgEleCho/EleCho.WpfSuite) [MessagePack](https://github.com/MessagePack-CSharp/MessagePack-CSharp)

## 预览
![playing](/Screenshot/playing.png)