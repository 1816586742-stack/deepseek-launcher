# ADR-002: ZCode 技能设计决策

**日期**: 2026-08-14
**状态**: 已批准
**决策者**: 项目维护者

## 背景

DSH Launcher 项目需要两个 ZCode 技能来辅助开发和项目管理。需要决定技能的架构、范围、触发条件等设计细节。

## 决策

### 1. 技能数量
两个技能分开:`dsh-launcher-dev`(开发)和 `github-project-mgmt`(项目管理)。职责清晰,不互相干扰。

### 2. 详细度
保持当前详细度(~80-100 行 SKILL.md),简洁实用。以后有需要再扩展,可以拆分成 SKILL.md + references/ 目录。

### 3. 测试策略
用今天的对话历史作为测试场景,覆盖实际使用情况。

### 4. 更新频率
项目有重大变更时同步更新技能,不设固定周期。

### 5. 存放位置
用户目录(`~/.agents/skills/`),跨项目可用。

### 6. 触发条件
宽泛触发,dsh-launcher-dev 的 description 覆盖多种相关场景,避免漏触发。

### 7. 输出格式
自由格式,根据具体任务决定输出结构。

### 8. 依赖关系
两个技能独立使用,没有依赖关系。

## 技能清单

| 技能 | 文件 | 主要功能 |
|---|---|---|
| `dsh-launcher-dev` | `~/.agents/skills/dsh-launcher-dev/SKILL.md` | 构建/测试/修复/添加功能 |
| `github-project-mgmt` | `~/.agents/skills/github-project-mgmt/SKILL.md` | Issue/PR/Release/标签管理 |

## 后果

### 正面
- 职责清晰,易于维护
- 跨项目可用
- 触发可靠

### 负面
- 两个技能可能有重叠内容(如 Release 管理)
- 需要同步更新

## 相关决策
- ADR-001: DSH Launcher 架构决策
