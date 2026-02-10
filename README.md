# 🎮 Cube World — Навигация

## 📚 Документация проекта

### Главные файлы
- **[PROJECT_KNOWLEDGE.md](PROJECT_KNOWLEDGE.md)** — полная база знаний проекта
- **[cube_world_plan.md](cube_world_plan.md)** — оригинальный план разработки
- **[my-agent/](my-agent/)** — мультиагентная система (10 агентов)

### Для быстрого старта
1. Прочитайте **[PROJECT_KNOWLEDGE.md](PROJECT_KNOWLEDGE.md)**
2. Откройте **[my-agent/INDEX.md](my-agent/INDEX.md)**
3. Начните с **[my-agent/QUICK_START.md](my-agent/QUICK_START.md)**

### Для работы с AI
- **[my-agent/PROMPTS_CHEATSHEET.md](my-agent/PROMPTS_CHEATSHEET.md)** — готовые промпты
- **[my-agent/AGENT_MAP.md](my-agent/AGENT_MAP.md)** — карта агентов

---

## 🤖 Мультиагентная система

### 10 специализированных агентов

| Агент | Фаза | Приоритет | Папка |
|-------|------|-----------|-------|
| Orchestrator | Все | 🔴 Критический | [orchestrator/](my-agent/orchestrator/) |
| VoxelEngine | 1 | 🔴 Критический | [voxel-engine/](my-agent/voxel-engine/) |
| PlayerController | 2 | 🔴 Критический | [player-controller/](my-agent/player-controller/) |
| CombatSystem | 4 | 🔴 Критический | [combat-system/](my-agent/combat-system/) |
| EnemyAI | 5 | 🔴 Критический | [enemy-ai/](my-agent/enemy-ai/) |
| VisualStyle | 3 | 🟡 Важный | [visual-style/](my-agent/visual-style/) |
| RPGSystem | 6 | 🟡 Важный | [rpg-system/](my-agent/rpg-system/) |
| WorldContent | 7 | 🟢 Желательный | [world-content/](my-agent/world-content/) |
| Polish | 9 | 🟢 Желательный | [polish/](my-agent/polish/) |
| Multiplayer | 8 | ⚪ Опциональный | [multiplayer/](my-agent/multiplayer/) |

---

## 🎯 Быстрый старт

**Команда для начала:**
```
Orchestrator, начинаем проект Cube World!
Фаза: 0 (Подготовка)
```

**Порядок разработки:**
```
1. VoxelEngine (Фаза 1)     ← Начать здесь
2. PlayerController (Фаза 2)
3. CombatSystem (Фаза 4)
4. EnemyAI (Фаза 5)
   ↓ MVP готов!
5. VisualStyle (Фаза 3)
6. RPGSystem (Фаза 6)
   ↓ Alpha готов!
7. WorldContent (Фаза 7)
8. Polish (Фаза 9)
   ↓ Beta готов!
9. Multiplayer (Фаза 8)
   ↓ Release!
```

---

## 📁 Структура проекта

```
D:\projects\CubeWorld\
├── README.md                   # Этот файл
├── PROJECT_KNOWLEDGE.md        # База знаний
├── cube_world_plan.md          # План разработки
│
├── my-agent/                   # Мультиагентная система
│   ├── INDEX.md                # Навигация
│   ├── README.md               # Обзор
│   ├── QUICK_START.md          # Быстрый старт
│   ├── PROMPTS_CHEATSHEET.md   # Промпты для AI
│   └── AGENT_MAP.md            # Карта агентов
│
└── Cube/                       # Unity проект
    ├── Assets/
    ├── ProjectSettings/
    └── ...
```

---

## ⏱️ Оценка времени

**С вайбкодингом (AI):** 3-5 месяцев  
**Без AI:** 12-20 месяцев

---

## 📞 Поддержка

Если возникли вопросы:
1. Проверьте **[PROJECT_KNOWLEDGE.md](PROJECT_KNOWLEDGE.md)**
2. Откройте **[my-agent/INDEX.md](my-agent/INDEX.md)**
3. Обратитесь к **Orchestrator Agent**

---

*Готово к использованию! 🚀*