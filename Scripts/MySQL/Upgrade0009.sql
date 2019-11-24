CREATE TABLE `creditCategory` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `name` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `description` varchar(4000) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `urls` mediumtext COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tags` mediumtext COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `alternateNames` mediumtext COLLATE utf8mb4_unicode_ci DEFAULT NULL,  
  PRIMARY KEY (`id`),
  KEY `ix_creditCategory_roadieId` (`roadieId`) 
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE `credit` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `artistId` int(11) NULL DEFAULT NULL,
  `releaseId` int(11) NULL DEFAULT NULL,
  `trackId` int(11) NULL DEFAULT NULL,  
  `creditCategoryId` int(11) NOT NULL,
  `isLocked` tinyint(1) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `creditToName` varchar(500) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `description` varchar(4000) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `urls` mediumtext COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `tags` mediumtext COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_credit_roadieId` (`roadieId`),
  KEY `idx_creditCreditandRelease` (`releaseId`,`id`),
  KEY `idx_creditCreditandTrack` (`trackId`,`id`),
  CONSTRAINT `credit_artist_ibfk_1` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE,  
  CONSTRAINT `credit_release_ibfk_1` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE,
  CONSTRAINT `credit_track_ibfk_1` FOREIGN KEY (`trackId`) REFERENCES `track` (`id`) ON DELETE CASCADE,
  CONSTRAINT `credit_category_ibfk_1` FOREIGN KEY (`creditCategoryId`) REFERENCES `creditCategory` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=0 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `creditCategory` VALUES (null,0,1,'d4008de3-2735-4991-968b-b6dfe868990a', UTC_TIMESTAMP(), null, 'Vocals', 'Provided lead or backup vocals',null,null,null);
INSERT INTO `creditCategory` VALUES (null,0,1,'aea463b8-0b90-47d5-8fde-5e68d3c454ed', UTC_TIMESTAMP(), null, 'Instrument', 'Played an instrument',null,null,null);
INSERT INTO `creditCategory` VALUES (null,0,1,'e3178aae-2359-4654-ab29-b45338b5984f', UTC_TIMESTAMP(), null, 'Production', 'Provided some role in production',null,null,null);

