Feature: Feature in project root

Scenario: SpecFlow glue files are generated
	Given I am curious
	When I request the version
	Then the result is content

Scenario Outline: Echo
	Given I am curious
	When I yell '<exclamation>'
	Then I hear '<exclamation>' echoed back

	Examples: 
		| exclamation    |
		| Yodelay-yi-hoo |
		| Helloooo       |