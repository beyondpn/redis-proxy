# redis-proxy
连接到 redis 的时候自动选择一个 redis db

开发环境中需要隔离大家的 redis 环境，在 windows 上开发时 windows 版的 redis 不稳定，经常碰到一些问题。为了保持配置文件的一致，小了这个小代理，可以在只有一个远程 redis 进程的情况下，满足隔离环境的需要。
