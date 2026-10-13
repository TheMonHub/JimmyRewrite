INSERT INTO cases (id, gid, uid, type, rid, rule_title, mod_uid, note, time, tban_expire, dm_success, img)
VALUES ((SELECT curr_cid + 1 FROM guild_dat WHERE gid = @GuildId), @GuildId, @UserId, @Type, @RuleId, @RuleTitle, @ModUid, @Note, @Time, @TBanExpire, @DMSuccess, @Img);
UPDATE guild_dat SET curr_cid = curr_cid + 1 WHERE gid = @GuildId;
