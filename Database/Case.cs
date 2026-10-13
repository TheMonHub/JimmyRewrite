// SPDX-License-Identifier: Apache-2.0
// Copyright (c) 2026 TheMonHub

namespace JimmyRewrite.Database;

public record Case(
    long Id,
    ulong UserId,
    ModerationHandler.PunishmentType PunishmentType,
    int[] RuleIds,
    string RuleTitle,
    ulong ModUid,
    string? Note,
    long Timestamp,
    long? TempBanExpire,
    bool? DMSuccess,
    string? Image = null);
