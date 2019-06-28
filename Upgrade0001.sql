-- Add new table to existing user table if not already added for < 1.0.0.5
ALTER TABLE `user` ADD COLUMN IF NOT EXISTS `lastFMSessionKey` varchar(50) NULL;