# TouchSocket.Dmtp

## 简介
DMTP（Duplex Message Transport Protocol 双工消息传输协议）是一个简单易用、便捷高效，且易于扩展的二进制数据协议。目前基于该协议，已实现的功能包括：连接验证、同步 Id、Rpc（包括客户端请求服务器，服务器请求客户端、客户端请求客户端）、文件传输（包括客户端向服务器请求文件、客户端向服务器推送文件、服务器向客户端请求文件、服务器向客户端推送文件、客户端之间请求、推送文件）、Redis 等。

## 功能特性
- **双工通信**：支持客户端与服务器、客户端与客户端之间的双工消息传输，实现高效的数据交互。
- **丰富的功能集**：涵盖连接验证、同步 Id、Rpc 调用、文件传输、Redis 集成等多种实用功能。
- **易于扩展**：协议设计灵活，方便开发者根据需求进行功能扩展和定制。

## 支持的目标框架
- net462
- net472
- net481
- netstandard2.0
- netstandard2.1
- net6.0
- net9.0
- net8.0

## 使用方法

请参阅[说明文档(https://touchsocket.net/)](https://touchsocket.net/)