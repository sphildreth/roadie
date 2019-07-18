-- Add new table for https://github.com/sphildreth/roadie-vuejs/issues/39
CREATE TABLE `inviteToken` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `createdByUserId` int(11) NOT NULL,
  `expiresDate` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_inviteToken_roadieId` (`roadieId`),
  CONSTRAINT `inviteToken_fk_1` FOREIGN KEY (`createdByUserId`) REFERENCES `user` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci