-- 用户表
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    phone VARCHAR(20),
    password_hash VARCHAR(255) NOT NULL,
    nickname VARCHAR(50) NOT NULL,
    avatar VARCHAR(255) DEFAULT 'default',
    level INTEGER DEFAULT 1,
    exp BIGINT DEFAULT 0,
    chips BIGINT DEFAULT 10000,
    diamonds BIGINT DEFAULT 0,
    vip_level INTEGER DEFAULT 0,
    status INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 用户统计表
CREATE TABLE IF NOT EXISTS user_stats (
    user_id UUID PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
    hands_played BIGINT DEFAULT 0,
    hands_won BIGINT DEFAULT 0,
    total_winnings BIGINT DEFAULT 0,
    total_losses BIGINT DEFAULT 0,
    biggest_pot BIGINT DEFAULT 0,
    win_rate DECIMAL(5,4) DEFAULT 0,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 游戏记录表
CREATE TABLE IF NOT EXISTS game_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    room_id VARCHAR(50) NOT NULL,
    hand_number INTEGER NOT NULL,
    buy_in BIGINT NOT NULL,
    cash_out BIGINT NOT NULL,
    profit BIGINT NOT NULL,
    hands_played INTEGER DEFAULT 0,
    hands_won INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 手牌历史表
CREATE TABLE IF NOT EXISTS hand_histories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_id VARCHAR(50) NOT NULL,
    hand_number INTEGER NOT NULL,
    players_data JSONB NOT NULL,
    board_cards JSONB,
    actions JSONB,
    pot_size BIGINT NOT NULL,
    winners JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 好友关系表
CREATE TABLE IF NOT EXISTS friendships (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    friend_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    status INTEGER DEFAULT 0, -- 0=pending, 1=accepted, 2=blocked
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, friend_id)
);

-- 用户道具表
CREATE TABLE IF NOT EXISTS user_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    item_id VARCHAR(50) NOT NULL,
    item_type VARCHAR(50) NOT NULL,
    quantity INTEGER DEFAULT 1,
    equipped BOOLEAN DEFAULT FALSE,
    expires_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 每日任务定义表
CREATE TABLE IF NOT EXISTS daily_tasks (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    task_type VARCHAR(50) NOT NULL,
    target INTEGER NOT NULL,
    reward BIGINT NOT NULL,
    reward_type VARCHAR(50) DEFAULT 'chips'
);

-- 用户任务进度表
CREATE TABLE IF NOT EXISTS user_tasks (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    task_id VARCHAR(50) NOT NULL REFERENCES daily_tasks(id),
    progress INTEGER DEFAULT 0,
    completed BOOLEAN DEFAULT FALSE,
    claimed BOOLEAN DEFAULT FALSE,
    date DATE NOT NULL DEFAULT CURRENT_DATE,
    PRIMARY KEY (user_id, task_id, date)
);

-- 成就定义表
CREATE TABLE IF NOT EXISTS achievements (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    category VARCHAR(50),
    requirement JSONB,
    reward BIGINT DEFAULT 0,
    icon VARCHAR(255)
);

-- 用户成就表
CREATE TABLE IF NOT EXISTS user_achievements (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    achievement_id VARCHAR(50) NOT NULL REFERENCES achievements(id),
    unlocked_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, achievement_id)
);

-- 签到记录表
CREATE TABLE IF NOT EXISTS sign_in_records (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    sign_date DATE NOT NULL DEFAULT CURRENT_DATE,
    consecutive_days INTEGER DEFAULT 1,
    reward BIGINT NOT NULL,
    PRIMARY KEY (user_id, sign_date)
);

-- 充值记录表
CREATE TABLE IF NOT EXISTS recharge_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    amount DECIMAL(10,2) NOT NULL,
    chips BIGINT NOT NULL,
    diamonds BIGINT DEFAULT 0,
    payment_method VARCHAR(50),
    transaction_id VARCHAR(100),
    status INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_chips ON users(chips DESC);
CREATE INDEX IF NOT EXISTS idx_game_records_user_id ON game_records(user_id);
CREATE INDEX IF NOT EXISTS idx_game_records_created_at ON game_records(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_hand_histories_room_id ON hand_histories(room_id);
CREATE INDEX IF NOT EXISTS idx_friendships_user_id ON friendships(user_id);
CREATE INDEX IF NOT EXISTS idx_friendships_friend_id ON friendships(friend_id);
CREATE INDEX IF NOT EXISTS idx_user_items_user_id ON user_items(user_id);

-- 插入默认每日任务
INSERT INTO daily_tasks (id, name, description, task_type, target, reward, reward_type) VALUES
('play_5_hands', '参与5手牌局', '完成5手牌局游戏', 'hands_played', 5, 100, 'chips'),
('win_3_hands', '赢得3手牌局', '赢得3手牌局', 'hands_won', 3, 200, 'chips'),
('play_30_min', '游戏30分钟', '累计游戏时长30分钟', 'play_time', 30, 150, 'chips'),
('win_big_pot', '赢得大底池', '赢得1000以上的底池', 'big_pot', 1, 300, 'chips'),
('play_with_friends', '与好友游戏', '与好友同桌游戏一局', 'friend_game', 1, 250, 'chips')
ON CONFLICT (id) DO NOTHING;

-- 插入默认成就
INSERT INTO achievements (id, name, description, category, requirement, reward) VALUES
('first_win', '初出茅庐', '赢得第一手牌', 'beginner', '{"hands_won": 1}', 100),
('win_10', '小有成就', '累计赢得10手牌', 'beginner', '{"hands_won": 10}', 500),
('win_100', '身经百战', '累计赢得100手牌', 'intermediate', '{"hands_won": 100}', 2000),
('royal_flush', '皇家同花顺', '打出皇家同花顺', 'hand', '{"hand_type": "royal_flush"}', 10000),
('straight_flush', '同花顺', '打出同花顺', 'hand', '{"hand_type": "straight_flush"}', 5000),
('four_of_kind', '四条', '打出四条', 'hand', '{"hand_type": "four_of_kind"}', 2000),
('millionaire', '百万富翁', '筹码达到100万', 'wealth', '{"chips": 1000000}', 5000),
('social_butterfly', '社交达人', '添加10个好友', 'social', '{"friends": 10}', 1000)
ON CONFLICT (id) DO NOTHING;
