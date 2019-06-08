-- Add new table to existing user table if not already added
ALTER TABLE `user` ADD COLUMN IF NOT EXISTS `lastFMSessionKey` varchar(50) NULL;