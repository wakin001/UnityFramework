#!/usr/bin/env ruby
# coding: utf-8
require 'fileutils'
require 'yaml'
require 'erb'

#Encoding.default_external = 'UTF-8'

class EnumTemplate < UnityClientTemplate
  TEMPLATE_PATH = 'enum.erb'

  def render(enum)
    super(enum: enum)
  end

  def emit(namespace, name, body)
    result = render(namespace: namespace, name: name, body: body)

    target_dir = @dst_dir

    if not File.directory?(target_dir)
      $logger.puts "creating output directory at #{target_dir}"
      FileUtils.mkdir_p(target_dir, :mode => 0755)
    end

    path = File.join(target_dir, name.to_s + ".cs")

    File.write(path, result)

    $logger.debug "(enum) #{name} -> #{path} (#{(result.size / 1000.0).round(3)} kB)"
  end
end

class UnityEnumGenerator
  def run(dst_dir)
    $logger.puts "---- unity client generator: destination '#{dst_dir}"

    _setup_params(dst_dir)

    @template_cache = TemplateCache.new(dst_dir)

    generated_count = 0

    perf = Benchmark.realtime do
      enums = {}
      @toplevel.each_master_schemes do |scheme|
        scheme.enums.each do |name, body|
          next if enums.has_key?(name)
          enums[name] = body
        end
      end
      enums.each do |name, body|

        @template_cache.load(EnumTemplate).emit(@toplevel.namespace_of_enum, name.to_s.sub(/^e/, ''), body)

        generated_count += 1

        GC.start
      end

      $logger.puts "generated #{generated_count} enum defines"

    end

    $logger.puts "code generation finished in #{(perf * 1000).round} ms"

    GC.start
  end

  def initialize(toplevel)
    @toplevel = toplevel
  end

  private
  def _setup_params(dst_dir)
    if not File.directory?(dst_dir)
      $logger.puts 'creating directory ' + dst_dir
      FileUtils.mkdir_p(dst_dir, :mode => 0775)
    end

    @dst_dir = dst_dir
  end
end

# enum 定義の csharp ソースファイルを生成する
def gen_enum(setting)
  dsl = parse_dsl(setting["source_dsl"])

  unity_client_output_directory = setting["enum_source_dir"]
  unity_client_generator = UnityEnumGenerator.new(dsl)
  unity_client_generator.run(unity_client_output_directory)
  unity_client_generator = nil
end

if __FILE__ == $0
  setting = YAML.load_file(ARGV[0])
  gen_enum(setting)
end
