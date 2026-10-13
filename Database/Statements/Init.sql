PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;
PRAGMA synchronous = NORMAL;
PRAGMA cache_size = -65536;
PRAGMA temp_store = MEMORY;

CREATE TABLE IF NOT EXISTS guild_dat (
    gid INTEGER PRIMARY KEY,
    curr_cid INTEGER NOT NULL DEFAULT 0,
    curr_rid INTEGER NOT NULL DEFAULT 0
) STRICT;

CREATE TABLE IF NOT EXISTS rules (
    gid INTEGER NOT NULL,
    rid INTEGER NOT NULL,
    title TEXT NOT NULL,
    desc TEXT,
    color TEXT NOT NULL,
    img TEXT,
    PRIMARY KEY (gid, rid),
    FOREIGN KEY (gid) REFERENCES guild_dat (gid) ON DELETE CASCADE
) STRICT;

CREATE TABLE IF NOT EXISTS conf (
    gid INTEGER PRIMARY KEY,
    message INTEGER,
    welcome INTEGER,
    watch INTEGER,
    mod INTEGER,
    appeal_link TEXT,
    force_note INTEGER NOT NULL DEFAULT 0,
    allow_no_rule INTEGER NOT NULL DEFAULT 0,
    mod_action_confirm INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (gid) REFERENCES guild_dat (gid) ON DELETE CASCADE
) STRICT;

CREATE TABLE IF NOT EXISTS cases (
    id INTEGER NOT NULL,
    gid INTEGER NOT NULL,
    uid INTEGER NOT NULL,
    type INTEGER NOT NULL,
    rid TEXT NOT NULL,
    rule_title TEXT NOT NULL,
    mod_uid INTEGER NOT NULL,
    note TEXT,
    time INTEGER NOT NULL,
    tban_expire INTEGER,
    dm_status BOOLEAN,
    img TEXT,
    PRIMARY KEY (id, gid),
    FOREIGN KEY (gid) REFERENCES guild_dat (gid) ON DELETE CASCADE
) STRICT;

CREATE TABLE IF NOT EXISTS tbans (
    gid INTEGER NOT NULL,
    uid INTEGER NOT NULL,
    expire INTEGER NOT NULL,
    PRIMARY KEY (gid, uid),
    FOREIGN KEY (gid) REFERENCES guild_dat (gid) ON DELETE CASCADE
) STRICT;

CREATE TABLE IF NOT EXISTS members (
    uid INTEGER NOT NULL,
    gid INTEGER NOT NULL,
    watch INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (uid, gid),
    FOREIGN KEY (gid) REFERENCES guild_dat (gid) ON DELETE CASCADE
) STRICT;

CREATE UNIQUE INDEX IF NOT EXISTS members_gid_uid ON members (gid, uid);

CREATE TABLE IF NOT EXISTS member_links (
    uid INTEGER NOT NULL,
    gid INTEGER NOT NULL,
    link_uid INTEGER NOT NULL,
    PRIMARY KEY (uid, gid, link_uid),
    FOREIGN KEY (uid, gid) REFERENCES members (uid, gid) ON DELETE CASCADE,
    FOREIGN KEY (link_uid, gid) REFERENCES members (uid, gid) ON DELETE CASCADE
) STRICT;

PRAGMA user_version = @DatabaseVersion;
