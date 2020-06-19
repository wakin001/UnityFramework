#!/usr/bin/env ruby
# coding: utf-8
require 'fileutils'
require 'optparse'
require 'yaml'

if __FILE__ == $0
  setting=YAML.load_file(ARGV[0])
end

MASTER_SOURCE_DIR = File.absolute_path(setting["master_source_dir"])
DSL_SOURCE_DIR = File.absolute_path(setting["dsl_source_dir"])
POLICO_SRC_DIR = File.absolute_path(setting["polico_source_dir"])
WORK_DIR = File.absolute_path(setting["work_dir"])

if __FILE__ == $0
  params = {"command" => "gen_client"}
  $params = params

  FileUtils.rm_rf WORK_DIR
  FileUtils.mkdir WORK_DIR

  require "#{POLICO_SRC_DIR}/main.rb"
  require_relative 'gen_enum.rb'
  require_relative 'cute_extension.rb'

  $logger = LoggerRepository.new(params["verbose"])
  params["source_dsl"] = DSL_SOURCE_DIR + "/dsl_main.dsl"
  params["output"] = WORK_DIR
  params["enum_source_dir"] = setting["enum_source_dir"]
  begin
    # polico/src/main.rb の gen_client() 呼出し
    gen_client(params)
    # makeEnum.rb の gen_enum() 呼出し
    gen_enum(params) if setting["enum_source_dir"]
  rescue Exception => e
    puts e.inspect
    puts e.backtrace.join("\n")
  end

  Dir.glob("#{WORK_DIR}/Client/*").each do |file|
    FileUtils.cp file, "#{MASTER_SOURCE_DIR}/#{File.basename(file)}"
  end
end
