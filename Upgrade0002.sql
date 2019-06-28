-- Change default character set and coallate to utcmb4 for < 1.0.1.0 database
alter table artist convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table artistAssociation convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table artistGenreTable convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table bookmark convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table chatMessage convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table collection convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table collectionrelease convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table genre convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table image convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table label convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table playlist convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table playlisttrack convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table `release` convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table releaseGenreTable convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table releaselabel convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table releasemedia convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table request convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table scanHistory convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table submission convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table track convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table trackPlaylistTrack convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table `user` convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table userClaims convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table userQue convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table userRoleClaims convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table userartist convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table userrelease convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table userrole convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table usersInRoles convert to character set utf8mb4 collate utf8mb4_unicode_ci;
alter table usertrack convert to character set utf8mb4 collate utf8mb4_unicode_ci;

-- Add new Comment table to < 1.0.1.0 database
CREATE TABLE `comment` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `replyToCommentId` int(11) DEFAULT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) NOT NULL, 
  `artistId` int(11) DEFAULT NULL,
  `collectionId` int(11) DEFAULT NULL,
  `genreId` int(11) DEFAULT NULL,
  `labelId` int(11) DEFAULT NULL,
  `playlistId` int(11) DEFAULT NULL,
  `releaseId` int(11) DEFAULT NULL,
  `trackId` int(11) DEFAULT NULL, 
  `comment` varchar(2500) NOT NULL,
  PRIMARY KEY (`id`),
  KEY `ix_request_roadieId` (`roadieId`),
  KEY `commentuser_ibfk_1` (`userId`),
  KEY `commentartist_ibfk_1` (`artistId`),
  KEY `commentcollection_ibfk_1` (`collectionId`),
  KEY `commentgenre_ibfk_1` (`genreId`),
  KEY `commentlabel_ibfk_1` (`labelId`),
  KEY `commentplaylist_ibfk_1` (`playlistId`),
  KEY `commentrelease_ibfk_1` (`releaseId`),
  KEY `commenttrack_ibfk_1` (`trackId`),
  CONSTRAINT `commentuser_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commentartist_ibfk_1` FOREIGN KEY (`artistId`) REFERENCES `artist` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commentcollection_ibfk_1` FOREIGN KEY (`collectionId`) REFERENCES `collection` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commentgenre_ibfk_1` FOREIGN KEY (`genreId`) REFERENCES `genre` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commentlabel_ibfk_1` FOREIGN KEY (`labelId`) REFERENCES `label` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commentplaylist_ibfk_1` FOREIGN KEY (`playlistId`) REFERENCES `playlist` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commentrelease_ibfk_1` FOREIGN KEY (`releaseId`) REFERENCES `release` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commenttrack_ibfk_1` FOREIGN KEY (`trackId`) REFERENCES `track` (`id`) ON DELETE CASCADE  
) ENGINE=InnoDB AUTO_INCREMENT=1  DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add new commentReaction table to < 1.0.1.0 database
CREATE TABLE `commentReaction` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `commentId` int(11) NOT NULL,
  `status` smallint(6) DEFAULT NULL,
  `roadieId` varchar(36) COLLATE utf8mb4_unicode_ci DEFAULT NULL, 
  `createdDate` datetime DEFAULT NULL,
  `lastUpdated` datetime DEFAULT NULL,
  `userId` int(11) NOT NULL,
  `reaction` enum('Dislike','Like') DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `commentReaction_userId_IDX` (`userId`,`commentId`) USING BTREE,
  KEY `ix_commentReaction_roadieId` (`roadieId`),
  KEY `commentReactionuser_ibfk_1` (`userId`),
  KEY `commentReactioncomment_ibfk_1` (`commentId`),
  CONSTRAINT `commentReactionuser_ibfk_1` FOREIGN KEY (`userId`) REFERENCES `user` (`id`) ON DELETE CASCADE,
  CONSTRAINT `commentReactioncomment_ibfk_1` FOREIGN KEY (`commentId`) REFERENCES `comment` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=1  DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

