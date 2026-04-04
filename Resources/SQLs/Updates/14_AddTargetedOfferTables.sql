CREATE TABLE IF NOT EXISTS `catalog_target_offers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `offer_code` varchar(64) NOT NULL,
  `catalog_item_id` int(11) NOT NULL,
  `credits` int(11) NOT NULL DEFAULT 0,
  `points` int(11) NOT NULL DEFAULT 0,
  `points_type` int(11) NOT NULL DEFAULT 0,
  `purchase_limit` int(11) NOT NULL DEFAULT 1,
  `end_timestamp` int(11) NOT NULL DEFAULT 0,
  `title` varchar(255) NOT NULL DEFAULT '',
  `description` text NOT NULL,
  `image` varchar(255) NOT NULL DEFAULT '',
  `icon` varchar(255) NOT NULL DEFAULT '',
  `variables` text DEFAULT NULL,
  `enabled` tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `offer_code` (`offer_code`),
  KEY `catalog_item_id` (`catalog_item_id`)
);

CREATE TABLE IF NOT EXISTS `users_target_offer_purchases` (
  `user_id` int(11) NOT NULL,
  `offer_id` int(11) NOT NULL,
  `state` int(11) NOT NULL DEFAULT 0,
  `amount` int(11) NOT NULL DEFAULT 0,
  `last_purchase` int(11) NOT NULL DEFAULT 0,
  PRIMARY KEY (`user_id`, `offer_id`),
  KEY `offer_id` (`offer_id`)
);
